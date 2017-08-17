# StoryTeller.TestRail.Sync
Small utility to keep your StoryTeller Specs in sync with your TestRail Cases. At this point the tool is very basic and simply meant to operate in "add or update" mode.

## Usage
The following steps will help you sync your local StoryTeller specs to your TestRail project.

### Pre-Requisites
This utility expects all of your StoryTeller specs that link to Cases in TestRail have their "C#" in the name of the spec in the following format: "[C#]". For example "My Spec [C1]" would link to Case C1 in TestRail.

### Syncing
The sync works by loading all the test cases in the TestRail project and then looking to see if the spec exists as a Case in TestRail. If the case exists the title will be updated if it has changed. Otherwise no update will occur. If the test case does not exist the spec will be added to TestRail and the newly created C# will be appended to the name of your spec and the spec file will be updated.

### Example
Below is the most basic example of how to trigger the sync with all required fields:

```StoryTeller.TestRail.Sync.exe --projectid myProjectId --username "username" --password "password" --testrailurl "https://yourproject.testrail.net" --specsfolder "C:\MySystem\Specs"```

If you don't want to pass your credentials you can also specify a text file that contains them. It expects the first line of the file to be the username and the second to be the password:

```StoryTeller.TestRail.Sync.exe --projectid myProjectId --credentialsfile "c:\myTestRailCreds.txt" --testrailurl "https://yourproject.testrail.net" --specsfolder "C:\MySystem\Specs"```

Lastly, there is a `--purge` flag that can be used **with _extreme_ caution** which will purge all Sections and Cases from the Project in TestRail. This can be handy when testing. If you run the utility with this flag it will require you to confirm before proceeding:

```StoryTeller.TestRail.Sync.exe --projectid myProjectId --username "username" --password "password" --testrailurl "https://yourproject.testrail.net" --specsfolder "C:\MySystem\Specs" --purge```