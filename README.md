# Moodle Homeworklog lib

A C# library, that lets you interact with (partially) the moodle backend and write to a log with a given id.

Libraries: 
- C# .Net Standard 2.0
- HtmlAgilityPack 1.11 (Provided by nuget package, must be included in all applications depending on this library)

## How to get it up and running

1. Clone repository `git clone https://github.com/siliwan/moodle-homework-log-lib`
2. Restore nuget packages
3. Build DLL

Then you can include the built DLL as a reference together with the HtmlAgilityPack DLL and start working with it.

## Example

This code shows how you can get and set your homework log in 3 steps

1. Login

```C#
using System.Net;
using Roamingstone.MoodleLog.HomeworkLog;

...

MoodleHomeworkLogManager homeworkLogManager = new MoodleHomeworkLogManager("https://moodle.example.com");

Cookie moodleSession = homeworkLogManager.Login("username", "password");

```

2. Get the sessionkey

```C#

string sessionKey = homeworkLogManager.GetSessKey(moodleSession);

```

3. Either get or set the content of the log

```C#

//Setting content
bool success = homeworkLogManager.SetLogContent(moodleSession, sessionKey, logId, "I will be written to the log!");

//You can also append to the content
bool success = homeworkLogManager.SetLogContent(moodleSession, sessionKey, logId, "I will be written to the log after the previous content!", true);

//Get the content
string content = homeworkLogManager.GetLogContent(moodleSession, logId);

```
