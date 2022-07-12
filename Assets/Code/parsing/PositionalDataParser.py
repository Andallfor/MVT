import csv
import sqlite3 as sql

conn = sql.connect("parsing/PositionalData.db")
cursor = conn.cursor()

command = '''
CREATE TABLE IF NOT EXISTS PositionalData (
    Date DOUBLE,
    X DOUBLE,
    Y DOUBLE,
    Z DOUBLE
)
'''
cursor.execute(command)
conn.commit()

with open('parsing/TESS.csv', 'r') as file:

    for line in file:   
        cursor.execute("INSERT INTO PositionalData VALUES (?,?,?,?)", line.split(",x"))

conn.commit()
conn.close()
        