A little while ago I came to the realization that Binge watching shows is inferior to weekly episodes and that every UI is clunky, and so PseudoVision was born.

# Clients
I've been kinda lazy on this front so bare with me, but you do have several options. Such as any video player that allows for URLs and any browser that supports the video element. I've created 2 possible URL formats
### Media Players:
http://{IP}:port/live/channelname
### Archives for MediaPlayers:
http://{IP}:port/archive/channelname/month/day/year
if there is an archive for that day it'll play from the begining
### Browsers:
http://{IP}:port/watch

## Working Clients
VLC Player

Windows Media PLayer

Edge and Chrome desktop Browser

# HLS Support
Most of my HLS testing as been with VLC, so I know for a fact that VLC is compatable with the way that the HLS manifest is delivered. 

The mini web player that I created (`index.html`) is not compatable. I'll be looking into implementing it for the future.

# Future endevures:
My original hope was to get it woring with UPNP and SSDP. Still in the works. Also a Roku channel would be nice as well. *is in the works.*
