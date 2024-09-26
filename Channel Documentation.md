This whole server is about Channels and Shows
# Channel.cs
`Channel` is an abstract class, just for the sole purpose of organizing the different `Schedule` making algorithms. Think of this as a `Show` array, not really though since the `Show[]` isn't stored in memory.
### `shows[]`:
is a readonly array that grabs anything in the channel's Shows folder.
This is just in case any new shows that get added using the ChannelManager while the actual Server program is running. I also didn't want to serialize the shows within the 'channel.chan' file before knowing if I was going to abstract the `Show` class.

## Methods
### `AddShow(Show newShow)`
This is to add any `Show` that you already have stored in memory, and was added for a `Binge_LikeChannel` feat. The show is reset and serialized in the target Channels folder

### `CreateNewSchedule(DateTime today)`
This is an abstract method. Use this to create your own Scheduling algorithm. the DateTime variable is used to make sure that you don't overwrite a previous `Schedule`.

### `Cancel(string name)`
```
public virtual void Cancel(string name)
{
    File.Delete(Path.Combine(ShowDirectory,name+"."+FileSystem.ShowEXT));
}
```
the `base` of the method simply removes the show file from the show directory. ***THIS DOES NOT DELETE THE ACTUAL MEDIA FILES, ONLY THE REFERENCE TO SAID FILES***

`TV_LikeChannel` is a bit more complex so it is a virtual method. 

### `static Channel Load(string path)`
Because of abstrtaction I decided to create a built in method for serializing the class. It reads the xml in the channel.chan file and determines if it's a Binge_Like or TV_Like.

# `BingeLike.cs`
*This is the simpler of the 2, so I'm gonna start with this one*

The overall Idea behind Binge_like is "What if Live TV only played new episodes, and I never missed those episodes".
## `ShowList`
The binge_like channle type outputs a `Showlist`. this is a class with a list of paths to the show files that are to be deserialized when needed. It inheirits from the interface `Ischedule` which is allows it to be serialized along side the `Schedule` class.

## "Boomeranging"
Upon a show ending, I found it appropriate to use the `Cancel()` method to keep reruns to a minimum. But also, I added 2 variables:

### `bool SendToNextChanWhenFinished`
You can toggle this to true or false, to tell it that you want it to `AddShow()`s to a different channel when they ended.
### `string NextChan`
This is the path to the target channel's `channel.chan file`

*this idea came from the same general concept of CartoonNetwork and Boomerang, this feature is only available on `Binge_LikeChannels`, and I have no current plan to add this to `TV_LikeChannel`*

# `TV_LikeChannel.cs`
*Straddel up because this one's a wild ride that's only getting more crazy*
This `Channel` type aims to create a list of Media items that's at least 23.9 hours long.
