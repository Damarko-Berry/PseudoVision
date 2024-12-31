A little while ago I came to the realization that Binge watching shows is inferior to weekly episodes and that every UI is clunky, and so PseudoVision was born.

# Clients
I've started on an "Official client"
https://github.com/Damarko-Berry/PV_Client

Which I have running on a rasberrypi for testing. Clone that repo and compile. You may need PVLib, https://github.com/Damarko-Berry/PseudoVision/releases/download/1.0.0/PVLib.zip 

Other working clients are any Videoplayer that's able to stream via HTTP.

### Media Players:
http://{IP}:port/live/channelname
### Archives for MediaPlayers:
http://{IP}:port/archive/channelname/month/day/year
if there is an archive for that day it'll play from the begining
### Browsers:
http://{IP}:port/watch

# HLS Support
Most of my HLS testing as been with VLC, so I know that VLC is compatable with the way that the HLS manifest is delivered. 

The mini web player that I created (`index.html`) is compatable.

# Future endevures:
My original hope was to get it woring with UPNP and SSDP. Still in the works. Also a Roku channel would be nice as well. *is in the works.*

More details on the inner workings of each class and function will be in the Wiki.
