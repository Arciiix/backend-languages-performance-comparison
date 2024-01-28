import json
import random
import time
import websocket
from threading import Thread
from typing import List
from sqlalchemy import create_engine, Column, Integer, select
from sqlalchemy.orm import sessionmaker, DeclarativeBase, Session
import pymysql


class Base(DeclarativeBase):
    pass


class Example(Base):
    __tablename__ = "Example"
    id = Column(Integer, primary_key=True)
    number = Column(Integer)


class TestOfNesting:
    def __init__(self, fibonacci_element_to_calculate: int):
        self.fibonacciElementToCalculate = fibonacci_element_to_calculate


class Request:
    def __init__(self, id: str, text_for_sorting: str, test_of_nesting: TestOfNesting):
        self.id = id
        self.textForSorting = text_for_sorting
        self.testOfNesting = test_of_nesting


class Result:
    def __init__(
        self,
        id: str,
        milliseconds_for_db: int,
        milliseconds_for_fibonacci: int,
        milliseconds_for_sorting: int,
    ):
        self.id = id
        self.millisecondsForDb = milliseconds_for_db
        self.millisecondsForFibonacci = milliseconds_for_fibonacci
        self.millisecondsForSorting = milliseconds_for_sorting


def calculate_fibonacci(n: int) -> int:
    if n <= 1:
        return n

    prev, current = 0, 1
    for _ in range(2, n + 1):
        prev, current = current, prev + current
    return current


def handle_socket_messages(
    ws: websocket.WebSocketApp, db_session: Session, db_connection
):
    def on_message(ws, message):
        data = json.loads(message)
        # Simple database query
        now = time.time()
        id_val = random.randint(1, 10)
        db_result = db_session.get(Example, id_val)
        time_taken_for_db = int((time.time() - now) * 1000)

        # Simple loop
        now = time.time()
        calculate_fibonacci(data["testOfNesting"]["fibonacciElementToCalculate"])
        time_taken_for_fibonacci = int((time.time() - now) * 1000)

        # Filter the string
        now = time.time()
        digits: List[int] = [
            int(char) for char in data["textForSorting"] if char.isdigit()
        ]

        # Sort an array
        digits.sort()
        time_taken_for_sorting = int((time.time() - now) * 1000)

        result = Result(
            data["id"],
            time_taken_for_db,
            time_taken_for_fibonacci,
            time_taken_for_sorting,
        )
        result_json = json.dumps(result.__dict__)

        ws.send(result_json)

    ws.on_message = on_message


def main():
    ws = websocket.WebSocketApp("ws://localhost:8181/ws")

    # Setup database connection
    db_connection = pymysql.connect(
        host="localhost",
        user="root",
        password="example",
        database="Test",
        charset="utf8mb4",
        cursorclass=pymysql.cursors.DictCursor,
    )

    engine = create_engine("mysql+pymysql://root:example@localhost/Test", echo=False)
    Session = sessionmaker(bind=engine)
    db_session = Session()

    def on_connected():
        print("Connected")

    ws.on_open = on_connected

    handle_socket_messages(ws, db_session, db_connection)
    ws_thread = Thread(target=ws.run_forever)
    ws_thread.start()

    print("Start")

    print("Press Enter to start")
    input()
    ws.send("python")
    ws_thread.join()


if __name__ == "__main__":
    main()
