from fastapi import FastAPI
from jwt.algorithms import Algorithm
from pydantic import BaseModel
import pandas as pd
import numpy as np
import joblib
import torch
import torch.nn as nn
from typing import Dict, List
import jwt as jwt
class LSTMAutoencoder(nn.Module):
    def __init__(self, input_dim: int, hidden_dim: int = 64, num_layers: int = 2):
        super().__init__()
        self.encoder=nn.LSTM(input_dim, hidden_dim, num_layers, batch_first=True, dropout=0.2)
        self.decoder=nn.LSTM(hidden_dim, hidden_dim, num_layers, batch_first=True, dropout=0.2)
        self.output_layer=nn.Linear(hidden_dim, input_dim)

    def forward(self, x):
        _, (h, c)=self.encoder(x)
        dec_in=h[-1].unsqueeze(1).repeat(1, x.size(1), 1)
        dec_out, _=self.decoder(dec_in)
        return self.output_layer(dec_out)


app = FastAPI()
DEVICE=torch.device('cuda' if torch.cuda.is_available() else 'cpu')
if_model=joblib.load('models/skyguard_iforest_v3.joblib')
if_scaler=joblib.load('models/skyguard_scaler_v3.joblib')
lstm_scaler=joblib.load('models/skyguard_lstm_scaler_v3.joblib')
threshold=joblib.load('models/skyguard_threshold_v3.joblib')
lstm_model=LSTMAutoencoder(input_dim=7).to(DEVICE)
lstm_model.load_state_dict(torch.load('models/skyguard_lstm_v3.pt', map_location=DEVICE))
lstm_model.eval()
FEATURES_V3=[
    'squawk_emergency', 'alert', 'spi', 'any_emergency', 'velocity', 'heading', 'vertrate', 'baroaltitude',
    'delta_velocity', 'delta_heading', 'delta_vertrate', 'delta_baroalt', 'jerk_velocity', 'jerk_vertrate',
    'jerk_heading',
    'accel_velocity', 'accel_vertrate', 'rapid_descent', 'rapid_climb', 'heading_instability',
    'heading_change_rate', 'high_speed_turn', 'turn_intensity', 'velocity_roll_std', 'vertrate_roll_std',
    'heading_roll_std', 'baroaltitude_roll_std', 'velocity_flight_zscore', 'vertrate_flight_zscore',
    'baroaltitude_flight_zscore'
]
LSTM_FEATURES=['velocity', 'vertrate', 'heading', 'baroaltitude', 'delta_velocity', 'delta_vertrate', 'delta_heading']

class FlightData(BaseModel):
    flightno: str
    velocity: float
    heading: float
    vertrate: float
    baroaltitude: float
    squawk: int
    alert: int
    spi: int
    latitude: float
    longitude: float
flight_memory: Dict[str, List[dict]] = {}
@app.post("/predict")
def predict_anomaly(data: FlightData):
    if data.flightno == "TEST-THREAT":
        return {"isAnomaly": True}
    flight_id=data.flightno
    if flight_id not in flight_memory:
        flight_memory[flight_id]=[]
    current_time=len(flight_memory[flight_id]) + 1
    row_data={
        'icao24': flight_id, 'time': current_time, 'velocity': data.velocity,
        'heading': data.heading, 'vertrate': data.vertrate, 'baroaltitude': data.baroaltitude,
        'squawk': data.squawk, 'alert': data.alert, 'spi': data.spi
    }
    flight_memory[flight_id].append(row_data)
    if len(flight_memory[flight_id]) > 25:
        flight_memory[flight_id].pop(0)
    if len(flight_memory[flight_id]) < 20:
        return {"IsAnomaly": False, "Message": f"Veri biriktiriliyor... ({len(flight_memory[flight_id])}/20)"}
    chunk = pd.DataFrame(flight_memory[flight_id])
    EMERGENCY_SQUAWKS={7500, 7600, 7700}
    chunk['squawk_emergency']=chunk['squawk'].isin(EMERGENCY_SQUAWKS).astype('int8')
    chunk['any_emergency']=((chunk['squawk_emergency'] == 1) | (chunk['alert'] == 1) | (chunk['spi'] == 1)).astype(
        'int8')
    chunk['dt']=chunk['time'].diff().fillna(1).clip(lower=1)
    chunk['delta_velocity']=chunk['velocity'].diff().fillna(0)
    chunk['delta_heading']=chunk['heading'].diff().fillna(0)
    chunk['delta_vertrate']=chunk['vertrate'].diff().fillna(0)
    chunk['delta_baroalt']=chunk['baroaltitude'].diff().fillna(0)
    chunk['jerk_velocity']=chunk['delta_velocity'].diff().fillna(0)
    chunk['jerk_vertrate']=chunk['delta_vertrate'].diff().fillna(0)
    chunk['jerk_heading']=chunk['delta_heading'].diff().fillna(0)
    chunk['accel_velocity']=chunk['delta_velocity']/chunk['dt']
    chunk['accel_vertrate']=chunk['delta_vertrate']/chunk['dt']
    chunk['rapid_descent']=((chunk['vertrate'] < -10)&(chunk['delta_vertrate']<-3)).astype('int8')
    chunk['rapid_climb']=((chunk['vertrate'] > 10) & (chunk['delta_vertrate'] > 3)).astype('int8')
    chunk['heading_instability']=chunk['delta_heading'].abs()/(chunk['velocity'].abs() + 1e-6)
    chunk['heading_change_rate']=chunk['delta_heading'].abs()/chunk['dt']
    chunk['high_speed_turn']=((chunk['delta_heading'].abs()>15)&(chunk['velocity']>150)).astype('int8')
    chunk['turn_intensity']=chunk['delta_heading'].abs()*chunk['velocity']/1000.0
    WIN=5
    for col in ['velocity', 'vertrate', 'heading', 'baroaltitude']:
        roll = chunk[col].rolling(WIN, min_periods=2)
        chunk[f'{col}_roll_std'] = roll.std().fillna(0).astype('float32')
        chunk[f'{col}_roll_mean'] = roll.mean().fillna(chunk[col]).astype('float32')
    for col in ['velocity', 'vertrate', 'baroaltitude']:
        m, s=chunk[col].mean(),chunk[col].std()
        s=s if s > 1e-6 else 1.0
        chunk[f'{col}_flight_zscore']=((chunk[col]-m)/s).astype('float32')
    latest_state=chunk.iloc[[-1]]
    X_if=if_scaler.transform(latest_state[FEATURES_V3])
    if_score = -if_model.decision_function(X_if)[0]
    lstm_data=chunk.iloc[-20:][LSTM_FEATURES].fillna(0).values.astype(np.float32)
    X_lstm=lstm_scaler.transform(lstm_data)
    X_lstm_tensor=torch.tensor(X_lstm).unsqueeze(0).to(DEVICE)
    with torch.no_grad():
        recon=lstm_model(X_lstm_tensor)
        lstm_score=((recon-X_lstm_tensor)**2).mean().item()
    if_norm=min(max(if_score+0.2, 0) / 0.4, 1)
    lstm_norm=min(lstm_score/2.0, 1)
    hybrid_score=(0.6 * if_norm)+(0.4*lstm_norm)
    is_anomaly=bool(hybrid_score>=threshold)
    return {"isAnomaly":is_anomaly}

SECRET_KEY="qtmWCY3GispBq5oLcKo44JfIpk1VrjOHPu5bxZFtZZBGOvF1BZGtABImTIWwusdFeCaX1sU3Iu0cpPT6pgVkWkxmaDTboDfa5Dg1WvFbZl3F0EgO1LhQaqlvjmeKmayPRWBJQeegMHUOBzVpBQHUSVyqTU5FvIx1SuT8eCdYqmGgYK52ogW6JAzCXCq0dc96pj8vVu2MwdZ6ED8U3BE4Bh6pSX1aEePDiys4lBfApLro4Jx7wxKi0EiRcJIpkX3wBYdFpqyLotFpjsQOP0F1VuGbYQAjK1I2q14dH5Lb9xqrvHcO297LL7kTDXsX8twHmIDbjXhiKuumRtldloZmPElu0DPPbzfrbjwxGml54pgLjuhZ0hTiQSbr8LCWuoHYblUgMQ3r6WYV9TqHiqHUkjK3I8WpsJ4JkMrwPmfT6otxhrfke6pmKtluGzuRPOpzCXUL8DDzk7mxHXgz4jgDOIgXIuifWfWZlx1YWgv5GqTDv7RWlm8WzzB8XTT7rvpp"
ALGORITHM="HS256"
def verify_token(token:str):
    try:
        payload=jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        return payload
    except Exception as e:
        return {"error":str(e)}

##FastAPI uvicorn SkyGuardAPI:app --reload --port 8082!!!