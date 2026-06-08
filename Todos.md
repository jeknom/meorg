# TODOs

## Image date inference improvement

**Issue:** Files where EXIF metadata has been stripped and where they have been passed around by copying, extracting from archive or downloaded: it's likely the filesystem creation time has been reset many times and is no longer reliable.

**Fix:** Check if the file modification time is before the file creation time and if so, use that to decide which directory the media file should be organized to.

## Progress indication

**Issue:** When processing large amounts of files the program can feel like it's stuck.

**Fix:** Check if the process is taking longer than ~2 seconds. If it is, start reporting amount of copied files whenever the count has changed and at least 2 seconds have passed. Upon exit, log status one last time.