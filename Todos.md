# TODOs

## Progress indication

**Issue:** When processing large amounts of files the program can feel like it's stuck.

**Fix:** Check if the process is taking longer than ~2 seconds. If it is, start reporting amount of copied files whenever the count has changed and at least 2 seconds have passed. Upon exit, log status one last time.