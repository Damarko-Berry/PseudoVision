A little while ago I came to the realization that Binge watching shows is inferior to weekly episodes and that every UI is clunky, and so PseudoVision was born.
##### The following Package is considered "portable".
# What is PseudoVision?
Pseudovision is a media server. That grabs the media you've given it and serves it to you at in a somewhat random fashion. As soon as you connect with any video player it grabs a file from a selection of Shows/movies and streams it to you.
Getting rid of the need of selection screens. The 3 projects above must be together. 
>PVLib: is the class library that the other 2 projects rely on. it holds the classes that I saw

>PseudoVision: is a console that is meant to run in the background

>PV Channel Manager is a the primary tool to be used. With it you're able to create Channels. Add and remove shows from channels. Turn the sever on and off.
# Before we start
## Folder setup:
Like other streaming servers all show folders must be set in a specific format to ensure all media is accounted for.
> I chose the classic format of Show>Season>file
> ![image](https://github.com/user-attachments/assets/2bb50e77-61d3-4271-8c4a-f6b3aa4cf56e)
## Shows
A class specifically made to keep track of what's been played
## Channels
These Control the behavior of a schedule. Each Channel's show list is independent of each other and can be on diferent episodes of the same show.
## Channel Types
Each Channel has different behaviors depending on the type you give it.
### TV_Like:
This was my original idea. This creates a 24hr schedule at the start of each day, and gives the client the scheduled media for that time of the day. You're also able to set a specific time for playing new episodes. Any time that falls outside of that hour of new episodes
will default to reruns. *exeption to this is the first time you create a schedule. As it'll add enough content comparable to the shows selected. roughly 21mins per show*.

Also to make sure that each show is on a weekly schedule, Up to 7 shows are selected to be played during the "Prime time" hour on the specific days, starting from saturday and going backwards. So, if you don't have a show for sunday, nothing new will play during PT.
> reruns are picked at random

>I reccomend using this for episodic shows like "Ed, Edd, n Eddy", "Scooby-doo" as missing an episode won't ruin the effect of the show)
### Binge_Like:
No Schedule is created at any point. You're simply connected to the next episode upon request. It selects a show at random to grab the next episode from.
> I recomend using this for serialized shows like most anime or any show that you wouldn't want to miss an episode of.

# Getting Started: 
Open the Channel manager.

> Click "New Channel"

>select a name and Channel Type.

>Select New Show *Feel free to select multiple Show-folders at once*

>Start Server

# Clients
I've been kinda lazy on this front so bare with me, but you do have several options. Such as any video player that allows for URLs and any browser that supports the video element. I've created 2 possible URL formats
### Media Players:
http://{IP}:6589/live/channelname
### Browsers:
http://{IP}:6589/watch/channelname

## Working Clients
VLC Player

Windows Media PLayer

Edge and Chrome desktop Browser

Phone Browsers

# Future endevures:
My original hope was to get it woring with UPNP and SSDP. Still in the works. Also a Roku channel would be nice as well. *is in the works.*

I also want to do "true live"
