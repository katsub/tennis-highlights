# Tennis Highlights

Tennis Highlights is an open-source .NET core application that processes homemade tennis videos and extracts the rallies segments, removing the sections where there is no play in the video. This can be done automatically or with some user input in a specialized interface that allows quick editing/selection of these rallies without much effort.

It can export all selected rallies to a single video, whose quality can be picked before exporting, so it'll have an appropriate size for sharing it.

How it works:
  - You pick a video and click on "Convert" so it extracts all rallies (takes a while, about the same time as the video duration or 30% of it, depending on your processor)
  - If you checked the "Join all rallies and export" option, the program will automatically build a video with all the rallies it detected and export it, and that's it!
  - If you didn't, it'll take you to the rally selection screen, where you can pick and/or edit the rallies that interest you, and the export them into a single video
  
*Remark*
  
The objective of this project is to remove the most downtime possible from tennis videos and extract rallies that are "easy to extract" (ball clearly visible, no camera shaking, etc). Properly segmenting all the rallies, although desirable, is not currently possible. For example, for a 40 minute-video with 60 rallies, the current version might detect 100 rallies (60 real ones and 40 fakes, which might be  composed of birds flying across the screen, people playing in neighbour courts, etc). Filtering these rallies by duration leaves 30 rallies (28 real, 2 fake ones). There's probably room for improvement in the filtering algorithm, ir order to leave more true rallies and less fake rallies.
  
# Installing

You can download the latest version of the program [here](https://github.com/katsub/tennis-highlights/releases/download/1.24/TennisHighlights.zip)

You'll also need the FFmpeg executable in order to export the videos, you can download the last version [here](https://ffmpeg.zeranoe.com/builds/)

# Getting Started

1. Pick a file to convert and an output folder to store the conversion data with the top screen buttons. Click on "Convert"

2. After a little while (between 2 and 5 minutes), the program should be done and the file explorer should open on the output folder, containing a video with the first 5 minutes of the video you picked, but without the downtime.

3. That's it! You can now try some things:
    - Untick the "Stop at X minutes" checkbox, so the entire video will be converted
    - Untick the "Auto join all and export", so you can edit the rallies and pick which ones you want to keep
    - Open the settings.xml file and increase the number of "BallExtractionWorkers" and "FrameExtractionWorkers" until you stop seeing an improvement on the processing time
 
  *Rally Selection View*
  
  The right column contains a list of all rallies extracted and their durations. All checked rallies on that list will be kept on the exported video. 
  The left pane shows the selected rally on the list. It can be edited by dragging the left and right sliders which correspond to the start and end of that rally.
  There are a couple of buttons:
  - Export: exports the selected rally to a separate video
  - Split: splits the selected rally at the current play position
  - Join: joins the selected rally with the next one on the list
  - Increase/Decrease speed: increases or decreases the speed of the viewed rally (this won't change the speed of the exported video)
  
  Once you're done selecting the rallies, you can click on the "Join selected rallies" button so the final video is exported

# Building

The project can be built with Visual Studio 2019 Preview 8 or higher. No external dependencies are required.

# Contributing

If you're happy with the project and you'd like to support it, you can [make a donation here](https://www.paypal.com/donate/?token=qMxrkbZ7wdmH0MHlTh5IOgTXSlkgKpNcbSjluXLvRvypnu8EmN0ET_KXginl9WWpV2OkrW&country.x=FR&locale.x=FR)

# Acknowledgments

Application icon provided by Freepik
