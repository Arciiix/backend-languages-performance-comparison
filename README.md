# Back-end languages performance comparison

Comparing the performance of several popular back-end technologies

I tried my best to make the results as accurate as possible - all the implementations are ran in production modes, without unnecessary console logs, are tested on the same machine with the same background processes,etc.

# What does it test?

- number of handled WebSocket requests per second (max load)
- time for a request to be fulfilled

Every request tests:

- JSON serialization/deserialization
- Simple database query (using an ORM)
- Simple loop (calculating Fibonacci series without recursion - interative approach; in another function)
- Finding digits in string (simple filter)
- Sorting an array (of those digits)

The flow looks the following:

1. Deserialize the request
1. Make a random query to the database - select a random row with id randomly chosen from 0 to 9
1. Make a function containing a simple loop calculating the Fibonacci series element with index corresponding to the one from the request, and call it
1. Filter the string from the request - by getting the digits from it to an array/list
1. Sort this array/list
1. Serialize the response, sending the time it took for the database, Fibonacci, and sorting

## Notes

The database used in this test was a simple MariaDB database ran in a Docker container with a simple table containing: id, number (as you can see by looking at the models from the given implementations)

Calculate the time of a function execution using DateTime-related objects (not a built-in functions to calculate the performance, e.g. Stopwatch in C#) - to also test the DateTime-related methods in a given language

Thanks to the overload on my PC because of that services, I could test how the back-end technologies would behave on low-level servers and SBCs (single-board computers, like Raspberry Pi).
