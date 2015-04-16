# RedditBot
Makes SlackBot post the top post from worldnews to a Slack channel

# How to use
Add a new file called `API.fs` above your `Program.fs` and put in

```
module API

let url = "https://<your team>.slack.com/services/hooks/slackbot?token=<your token>&channel=%23<your channel without #>"
```

Compile and run.
