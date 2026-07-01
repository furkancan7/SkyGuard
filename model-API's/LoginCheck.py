import bcrypt
from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel
import sqlite3
from fastapi.middleware.cors import CORSMiddleware
import traceback

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class LoginCheck(BaseModel):
    username: str
    password: str


def get_db_connection():
    # Buraya kendi bilgisayarındaki SkyGuard.db dosyasının TAM YOLUNU yaz Furkan Can!
    # Ters eğik çizgileri ( \ ) düz çizgiye ( / ) çevirmeyi unutma.
    db_path = "C:/Users/hp/PycharmProjects/PythonProject/UsersSkyGuard.db"
    conn = sqlite3.connect(db_path, check_same_thread=False)
    return conn


@app.post("/Login")
async def login(request: LoginCheck):
    # Terminalde gelen isteği ve parametreleri görelim
    print(f"\n[FastAPI] Giriş İsteği Geldi! Kullanıcı: {request.username}")

    try:
        conn = get_db_connection()
        cursor = conn.cursor()

        # 1. Adım: Veritabanında tablo var mı, kullanıcı var mı kontrolü
        cursor.execute("SELECT password_hash FROM Users WHERE username = ?", (request.username,))
        user = cursor.fetchone()
        conn.close()

        if not user:
            print(f"[FastAPI] Kullanıcı bulunamadı: {request.username}")
            raise HTTPException(status_code=401, detail="Kullanıcı adı veya şifre hatalı.")

        db_password_hash = user[0]

        # 2. Adım: Veritabanından gelen veriyi güvenle byte dizisine çevirme
        if isinstance(db_password_hash, str):
            db_password_hash = db_password_hash.encode('utf-8')

        # 3. Adım: Bcrypt Şifre Doğrulama (En çok çökülen yer)
        try:
            if bcrypt.checkpw(request.password.encode("utf-8"), db_password_hash):
                print("[FastAPI] Şifre Doğru! Giriş Başarılı.")
                return {
                    'status': 'success',
                    'message': 'Giriş Başarılı',
                    'redirect': 'radar'
                }
            else:
                print("[FastAPI] Şifre Yanlış!")
                raise HTTPException(status_code=401, detail="Kullanıcı adı veya şifre hatalı.")
        except ValueError as b_ex:
            print(f"[FastAPI] Bcrypt Hatası (Muhtemelen veritabanındaki şifre hash'li değil, düz metin!): {b_ex}")
            raise HTTPException(status_code=401, detail="Veritabanındaki şifre formatı geçersiz (Hash değil)!")

    except Exception as ex:
        # EĞER KOD HERHANGİ BİR SEBEPLE ÇÖKERSE 500 VERMEDEN ÖNCE HATAYI TERMİNALE BASACAK:
        print("\n!!! PYTHON İÇ HATASI (HTTP 500 SEBEBİ) !!!")
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Python İç Hatası: {str(ex)}")

# Çalıştırmak için: uvicorn LoginCheck:app --reload --port 8081