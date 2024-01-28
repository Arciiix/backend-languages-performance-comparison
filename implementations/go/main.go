package main

import (
	"arciiix/backend-test/model"
	"encoding/json"
	"fmt"
	"log"
	"math/rand"
	"net/url"
	"os"
	"os/signal"
	"sort"
	"strconv"
	"time"
	"unicode"

	"github.com/gorilla/websocket"

	"gorm.io/driver/mysql"
	"gorm.io/gorm"
)

type TestOfNesting struct {
	FibonacciElementToCalculate int `json:"fibonacciElementToCalculate"`
}

type Request struct {
	Id             string        `json:"id"`
	TextForSorting string        `json:"textForSorting"`
	TestOfNesting  TestOfNesting `json:"testOfNesting"`
}

type Result struct {
	Id                       string `json:"id"`
	MillisecondsForDb        int    `json:"millisecondsForDb"`
	MillisecondsForFibonacci int    `json:"millisecondsForFibonacci"`
	MillisecondsForSorting   int    `json:"millisecondsForSorting"`
}

func calculateFibonacci(n int) int {
	if n <= 1 {
		return n
	}

	prev, current := 0, 1
	for i := 2; i <= n; i++ {
		prev, current = current, prev+current
	}
	return current
}

func handleSocketMessages(c *websocket.Conn, done chan struct{}, db *gorm.DB) {
	defer close(done)
	for {
		_, message, err := c.ReadMessage()
		if err != nil {
			log.Println("error reading:", err)
			return
		}

		// received a message
		data := Request{}
		err = json.Unmarshal(message, &data)
		if err != nil {
			log.Println("error parsing:", err)
		}

		// simple database query
		now := time.Now()
		id := rand.Intn(10)
		dbResult := model.Example{}
		db.First(&dbResult, id)
		timeTakenForDb := time.Now().UnixMilli() - now.UnixMilli()

		// Simple loop
		now = time.Now()
		calculateFibonacci(data.TestOfNesting.FibonacciElementToCalculate)
		timeTakenForFibonacci := time.Now().UnixMilli() - now.UnixMilli()

		// Filter the string
		now = time.Now()

		var digits []int

		for _, char := range data.TextForSorting {
			if unicode.IsDigit(char) {
				digit, _ := strconv.Atoi(string(char))
				digits = append(digits, digit)
			}
		}

		// Sort an array
		sort.Ints(digits)
		timeTakenForSorting := time.Now().UnixMilli() - now.UnixMilli()

		result := Result{
			Id:                       data.Id,
			MillisecondsForDb:        int(timeTakenForDb),
			MillisecondsForFibonacci: int(timeTakenForFibonacci),
			MillisecondsForSorting:   int(timeTakenForSorting),
		}

		resultToSend, err := json.Marshal(result)

		if err != nil {
			log.Fatal("serializing", err)
		}

		err = c.WriteMessage(websocket.TextMessage, resultToSend)

		if err != nil {
			log.Fatal("serializing", err)
		}
	}
}

func main() {
	rand.Seed(time.Now().UnixNano())

	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt)

	u := url.URL{Scheme: "ws", Host: "localhost:8181", Path: "/ws"}
	log.Printf("connecting to %s", u.String())

	c, _, err := websocket.DefaultDialer.Dial(u.String(), nil)
	if err != nil {
		log.Fatal("dial:", err)
	}
	defer c.Close()

	dsn := "root:example@tcp(127.0.0.1:3306)/Test?charset=utf8mb4&parseTime=True&loc=Local"
	db, err := gorm.Open(mysql.Open(dsn), &gorm.Config{})

	if err != nil {
		log.Fatal("db:", err)
	}

	done := make(chan struct{})

	go handleSocketMessages(c, done, db)

	log.Println("press enter to start")
	fmt.Scanln()

	log.Print("start")

	err = c.WriteMessage(websocket.TextMessage, []byte("golang"))
	if err != nil {
		log.Fatal("init:", err)
	}

	for {
		select {
		case <-done:
			return
		case <-interrupt:
			log.Println("interrupt")
			err := c.WriteMessage(websocket.CloseMessage, websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
			if err != nil {
				log.Println("write close:", err)
				return
			}
			select {
			case <-done:
			case <-time.After(time.Second):
			}
			return
		}
	}
}
