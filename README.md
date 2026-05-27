# QuestTracker
A FFXIV Dalamud plugin for the quest completionist

For any suggestions or issues, feel free to ping me @isaiahcat on the [XIVLauncher/Dalamud discord server](https://goat.place/).

### Overview
![Main](QuestTracker/images/overview.png)

### Quests
![Settings](QuestTracker/images/quests.png)

### Settings
![Settings](QuestTracker/images/settings.png)

## Fork
This fork is intended for updates and upgrades by @maxiin since the original seems to be abandoned.
Changes:
- [x] Update to 7.5
- [x] Create a "new quests system", so it gets from Lumina the quests that are missing in the data.json and lists it more easily for updates
- [x] Create a tab that lists quests by patch (So people can complete quests in order of release)
- [ ] Create a way to automatically get the patch the quests are coming from (Dunno if its needed since new quests are from the new patch)
- [ ] Fix Github actions so it creates the plugin files automatically and can be updated in the game with a custom repo instead of manually
- [ ] Search by quests id
- [ ] Fix input view sizes when window is resized
- [ ] Save last option selected
- [ ] Fix Scroll (It automatically goes to the middle of the list for some reason ???)
