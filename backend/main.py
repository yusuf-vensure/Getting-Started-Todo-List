import time
import redis
import mysql.connector
from flask_cors import CORS
from flask import Flask, request, jsonify


print("Waiting for MySQL to be ready...")

app = Flask(__name__)
CORS(app)

r = redis.Redis(host='redis', port=6379, decode_responses=True)

conn_str = (
    "DRIVER={ODBC Driver 18 for SQL Server};"
    "SERVER=db;"
    "DATABASE=db1;"
    "UID=sa;"
    "PWD=YourStrong!Password123;"
    "TrustServerCertificate=yes;"
)

db = None
while not db:
    try:
        db = pyodbc.connect(conn_str, autocommit=True)
        print("Connected to MySQL successfully!")
    except pyodbc.Error as e:
        print(f"Database not ready ({e}), retrying in 3 seconds...")
        time.sleep(3)

cursor = db.cursor()

print("Echo!")

cursor.execute("""
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name VARCHAR(100)
    )
END
""")

@app.route('/page-views')
def page_views():
    # Increment a page view counter stored in memory!
    views = r.incr('page_views')
    return f"This page has been viewed {views} times!"




@app.route('/api/stats')
def get_stats():
    # .incr() adds +1 to the 'page_views' key in Redis memory automatically
    views = r.incr('page_views')
    return jsonify({
        'message': 'Welcome to the Todo App!',
        'total_page_loads': views
    })



@app.route('/hello')
def hello():
    return "Live reload is working!"




@app.route("/add-user", methods=["POST"])
def add_user():
    name = request.form["name"]

    cursor.execute(
        "INSERT INTO users (name) VALUES (%s)",
        (name,)
    )
    db.commit()

    return f"Saved {name}!"


if __name__ == "__main__":
    app.run(host='0.0.0.0', port=5000, debug=True)