# Xpressive.Home

Home automation solution in .NET

## Scripts

### Variables

### Devices

Most gateways gives you the possibility to access the devices in JavaScript by its id.

```
// Daylight gateway
var daylightDevice = daylight("id");
var isDaylight = daylightDevice.isDaylight();

// Denon gateway
var denonDevice = denon("id");
denonDevice.on();
denonDevice.off();
var isMute = denonDevice.mute();
denonDevice.mute(true);
denonDevice.mute(false);
var selectedSource = denonDevice.source();
denonDevice.source("DVD");
var volume = denonDevice.volume();
denonDevice.volume(30);

// LIFX gateway
var lifxDevice = lifx("id");
lifxDevice.on();
lifxDevice.off();
lifxDevice.on(transitionTimeInSeconds);
lifxDevice.off(transitionTimeInSeconds);
lifxDevice.color("#ff0000");
lifxDevice.color("#ff0000", transitionTimeInSeconds);
lifxDevice.brightness(0.5);
lifxDevice.brightness(0.5, transitionTimeInSeconds);

// SONOS gateway
var sonosDevice = sonos("id");
sonosDevice.play();
sonosDevice.pause();
sonosDevice.stop();
var volume = sonosDevice.volume();
sonosDevice.volume(0.3);
var isMaster = sonosDevice.master();

if (sonosDevice.state() === 'PLAYING') {
    sonosDevice.stop();
}
```
