# NumbersServer
# .NET Numbers Server Code Challenge Instructions 

Write a server (“Application”) in C# or C++ that opens a TCP socket and restricts input to at most 5 concurrent clients. Clients will connect to the Application and write any number of 9 digit numbers, and then close the connection.
The application must write a de-duplicated list of these numbers to a log file in no particular order.

# Primary Considerations

1. The Application should work correctly as defined below in Requirements.
2. The overall structure of the Application should be simple.
3. The code of the Application should be descriptive and easy to read, and the build method and runtime parameters must be well-described and work.
4. The design should be resilient with regard to data loss.
5. The Application should be optimized for maximum throughput, weighed along with the other Primary Considerations and the Requirements below.

# Requirements

1. The Application must accept input from at most 5 concurrent clients on TCP/IP port 4000.
1. Input lines presented to the Application via its socket must either be composed of exactly nine decimal digits  (e.g., 314159265 or 007007009) immediately followed by a server-native newline sequence; or a termination sequence as detailed in (8.), below. Numbers presented to the Application must include leading zeros as necessary to ensure they are each 9 decimal digits.
1. The log file, to be named "numbers.log”, must be created anew and/or cleared when the Application starts.
Only numbers may be written to the log file. Each number must be followed by a server-native newline sequence.
1. No duplicate numbers may be written to the log file.
Any data that does not conform to a valid line of input should be discarded and the client connection terminated immediately and without comment.
1. Every 10 seconds, the Application must print a report to standard output:
    * The difference since the last report of the count of new unique numbers that have been received.
    * The difference since the last report of the count of new duplicate numbers that have been received.
    * The total number of unique numbers received for this run of the Application.
1. If any connected client writes a single line with only the word "terminate" followed by a server-native newline sequence, the Application must disconnect all clients and perform a clean shutdown as quickly as possible. 
1. Clearly state all of the assumptions you made in completing the Application.

Example report output:

> Received
>
> 50 unique numbers, 2 duplicates. Unique total: 567231

# Notes

When considering responses, New Relic will favor solutions that demonstrate low-level knowledge of threading and synchronization.

* Try to spend no more than 8 hours in total completing your solution. Reserve time to document your technical decisions, any known issues with your solution, and what next steps you would take if you had more time to spend on the challenge
* You may write tests at your own discretion. Tests are useful to ensure your Application passes Primary Consideration A.
* You are not restricted to the class libraries and frameworks that are considered a standard part of the language. You may use widely accepted libraries and frameworks such as NUnit, particularly if their use helps improve Application simplicity and readability.
* Your Application may not for any part of its operation use or require the use of external systems such as Memcache or Redis.
* At your discretion, leading zeroes present in the input may be stripped—or not used—when writing output to the log or console.
* Robust implementations of the Application typically handle more than 2M numbers per 10-second reporting period on a  modern high-performance laptop (e.g.: 16 GiB of RAM, SSD, and quad 2.5 GHz Intel i7 processors).

# Submission

In order to enable our engineers to review your submission anonymously, please do not include any identifying information (name, GitHub name, etc) in your source files.  You may submit your work via a mechanism of your choice such as GitHub, Email, Zipfile. However, to keep our interview questions private, we do ask that your solution not be publicly accessible if storing on an online repository. Regardless of your chosen mechanism, you should make sure
we have the necessary permissions to access your full solution and source files. Please include sufficient instructions for accessing and running your code.
