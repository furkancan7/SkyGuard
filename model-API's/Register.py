import sqlite3
import bcrypt
conn = sqlite3.connect('UsersSkyGuard.db')
cursor = conn.cursor()

cursor.execute('''
    CREATE TABLE IF NOT EXISTS Users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT NOT NULL UNIQUE,
        password_hash TEXT NOT NULL,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
''')

conn.commit()
print("Veritabanı ve Users tablosu başarıyla oluşturuldu.")

class Register():
  def register_user(username, password):
    salt = bcrypt.gensalt()
    hashed_password = bcrypt.hashpw(password.encode("utf-8"), salt)
    try:
      cursor.execute('''
                     INSERT INTO Users (username, password_hash, created_at)''')
      conn.commit()
    except sqlite3.IntegrityError:
      print("Hata!")
class LoginCheck():
  def login_check(username, input_password):
    cursor.execute("SELECT password_hash FROM Users WHERE username = ?", (username,))
    result = cursor.fetchone()

    if result:
      db_hash = result[0]
      if bcrypt.checkpw(input_password.encode('utf-8'), db_hash):
        return True, "Giriş Başarılı"
      else:
        return False, "Kullanıcı adı veya şifre hatalı"  # Kriptolu mesaj dönebilirsin
    return False, "Kullanıcı bulunamadı"
    success, message = Login_check("admin_emre", "GuvenliSifre123")