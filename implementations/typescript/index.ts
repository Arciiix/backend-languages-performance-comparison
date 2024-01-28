import WebSocket from "ws";
import readline from "readline";
import { PrismaClient } from "@prisma/client";

interface RequestPayload {
  id: string;
  textForSorting: string;
  testOfNesting: TestOfNesting;
}

interface TestOfNesting {
  fibonacciElementToCalculate: number;
}

interface Result {
  id: string;
  millisecondsForDb: number;
  millisecondsForFibonacci: number;
  millisecondsForSorting: number;
}

const prisma = new PrismaClient();

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
});
const url = "ws://0.0.0.0:8181";

const ws: WebSocket = new WebSocket(url);

function calculateFibonacci(n: number): number {
  if (n <= 1) {
    return n;
  }

  let fibPrev = 0;
  let fibCurr = 1;

  for (let i = 2; i <= n; i++) {
    const temp = fibCurr;
    fibCurr = fibPrev + fibCurr;
    fibPrev = temp;
  }

  return fibCurr;
}

ws.on("open", () => {
  console.log("Connected to WebSocket server");

  rl.question("Please enter any key: ", (key) => {
    ws.send("typescript");
    rl.close();
  });
});

ws.on("message", async (message: WebSocket.Data) => {
  // JSON deserialization
  const data: RequestPayload = JSON.parse(message);

  // Simple database query
  let now = new Date();
  await prisma.example.findFirst({
    where: { id: Math.floor(Math.random() * 10) },
  });
  const timeTakenForDb = new Date().getTime() - now.getTime();

  // Simple loop
  now = new Date();
  calculateFibonacci(data.testOfNesting.fibonacciElementToCalculate);
  const timeTakenForFibonacci = new Date().getTime() - now.getTime();

  // Filter the string
  const digits = Array.from(data.textForSorting)
    .map(Number)
    .filter((num) => !isNaN(num));

  // Sort an array
  digits.sort();
  const timeTakenForSorting = new Date().getTime() - now.getTime();

  // Serialize and send the result
  const result: Result = {
    id: data.id,
    millisecondsForDb: timeTakenForDb,
    millisecondsForFibonacci: timeTakenForFibonacci,
    millisecondsForSorting: timeTakenForSorting,
  };
  ws.send(JSON.stringify(result));
});

// Event listener for when the connection is closed
ws.on("close", () => {
  console.log("Disconnected from WebSocket server");
});
