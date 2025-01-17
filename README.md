A little while ago I came to the realization that Binge watching shows is inferior to weekly episodes and that every UI is clunky, and so PseudoVision was born.
## PseudoVision
This is a homebrew TV station that supports multiple "channles". Channels can use Livestreaming if you want them to.
### Warning
_Due to the way that I set up HLS, TV_Like channels that use HLS can take several minutes before they're availible to stream. More details about this are in the wiki._

## Clients
I've started on an ["Official client"](https://github.com/Damarko-Berry/PV_Client)

Which I have running on a rasberrypi for testing. Clone that repo and compile. You may need [PVLib](https://github.com/Damarko-Berry/PseudoVision/releases/download/1.0.1/PVLib.zip) 

Other working clients are any Videoplayer that's able to stream via HTTP.

### Media Players:
http://{IP}:port/live/channelname
### Archives for MediaPlayers:
http://{IP}:port/archive/channelname/month/day/year
if there is an archive for that day it'll play from the begining
### Browsers:
http://{IP}:port/watch

## Remote
[PV_Controller](https://github.com/Damarko-Berry/PV_Controller)

This does not connect with the server, but does work well with the client

# HLS Support
Most of my HLS testing as been with VLC, so I know that VLC is compatable with the way that the HLS manifest is delivered. 

The mini web player that I created (`index.html`) is compatable.

More details on the inner workings of each class and function will be in the Wiki.
