use std::io::{self, Write};
use std::sync::mpsc;
use std::thread;
use std::time::SystemTime;

use mysql::prelude::Queryable;
use mysql::{Pool, PooledConn};
use rand::{Error, Rng};
use serde::{Deserialize, Serialize};
use tungstenite::connect;
use tungstenite::Message;

#[derive(Debug, Deserialize)]
struct TestOfNesting {
    fibonacciElementToCalculate: i32,
}

#[derive(Debug, Deserialize)]
struct Request {
    id: String,
    textForSorting: String,
    testOfNesting: TestOfNesting,
}

#[derive(Debug, Serialize)]
struct ResultResponse {
    id: String,
    millisecondsForDb: i64,
    millisecondsForFibonacci: i64,
    millisecondsForSorting: i64,
}

struct Example {
    id: i32,
    number: i32,
}

fn calculate_fibonacci(n: i64) -> i64 {
    if n <= 1 {
        return n;
    }

    let (mut prev, mut current) = (0, 1);
    for _ in 2..=n {
        let next = prev + current;
        prev = current;
        current = next;
    }
    current
}

fn handle_socket_messages(message_str: &str, conn: &mut PooledConn) -> Result<ResultResponse, ()> {
    if let Ok(data) = serde_json::from_str::<Request>(message_str) {
        let now = SystemTime::now();

        // Simple database query
        let id: i32 = rand::thread_rng().gen_range(0..10);
        // NOTE: I'm not using Diesel because it requires additional MySQL library on the system
        let query = format!("SELECT id, number FROM Example WHERE id={}", id);
        let res = conn
            .query_map(query, |(id, number)| Example {
                id: id,
                number: number,
            })
            .expect("Query failed.");

        let milliseconds_for_db = now.elapsed().unwrap().as_millis();

        // Simple loop
        let now = SystemTime::now();
        calculate_fibonacci(data.testOfNesting.fibonacciElementToCalculate as i64);
        let milliseconds_for_fibonacci = now.elapsed().unwrap().as_millis();

        // Filter the string
        let now = SystemTime::now();
        let digits: Vec<_> = data
            .textForSorting
            .chars()
            .filter_map(|c| c.to_digit(10))
            .collect();

        // Sort an array
        let mut sorted_digits = digits.clone();
        sorted_digits.sort();
        let milliseconds_for_sorting = now.elapsed().unwrap().as_millis();

        let result = ResultResponse {
            id: data.id,
            millisecondsForDb: milliseconds_for_db as i64,
            millisecondsForFibonacci: milliseconds_for_fibonacci as i64,
            millisecondsForSorting: milliseconds_for_sorting as i64,
        };
        return Ok(result);
    }
    Err(())
}

fn main() {
    let (mut socket, _) = connect("ws://localhost:8181/ws").expect("Failed to connect");
    println!("Press Enter to start...");
    io::stdout().flush().unwrap();
    let mut input = String::new();
    io::stdin().read_line(&mut input).unwrap();

    println!("Starting...");

    let url = "mysql://root:example@localhost:3306/Test";
    let pool = Pool::new(url).unwrap();
    let mut conn = pool.get_conn().unwrap();

    socket.write_message(Message::Text("rust".into())).unwrap();

    loop {
        match socket.read_message() {
            Ok(result) => {
                let response =
                    handle_socket_messages(result.to_text().unwrap(), &mut conn).unwrap();
                let serialized = serde_json::to_string(&response).unwrap();
                socket
                    .write_message(Message::Text(serialized.into()))
                    .unwrap();
            }
            Err(_) => {
                break;
            }
        }
    }
}
