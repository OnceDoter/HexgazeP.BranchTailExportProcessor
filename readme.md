## Activity BRANCH tail sampler processor
### Brief opinion
1. Why, if there is an Otel Collector? - In really high-load applications, the Otel Collector is a bottleneck, and sending every activity to it is a great load on the network.
2. Why do we need every activity? Why do we need every activity? Most likely not why. For API performance statistics, we can only record root spans.
   For debugging errors - all activities with errors
   Etc.
### Description of our use case
1. All actions are dispatched only when the root action is received.
2. Always Dispatch Root Actions
3. Always send all actions (root and child) on request if:
   1. request ended with an error
   2. the request took longer than 2s
   3. the request is tagged with a debug tag in the code
   4. There is a chance to send the entire trace by default = 0.01 (1%)