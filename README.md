# Xpressive.Home

Home automation solution in .NET

## Scripts

### Variables

### Devices

Most gateways gives you the possibility to access the devices in JavaScript by its id.

```
variable.set("xpressive.home is cool", true);
var isCool = variable.get("xpressive.home is cool");

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

var denonDevices = denon_list.all();
var denonDevices = denon_list.byRoom("Living room");

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

var lifxDevices = lifx_list.all();
var lifxDevices = lifx_list.byRoom("Living room");

// myStrom gateway
var mystromDevice = mystrom("id");
mystromDevice.on();
mystromDevice.relay(true); // same as on();
mystromDevice.off();
mystromDevice.relay(false); // same as off();
var isOn = mystromDevice.relay();
var consumption = mystromDevice.power();

var mystromDevices = mystrom_list.all();
var mystromDevices = mystrom_list.byRoom("Living room");

// Netatmo gateway
var netatmoDevice = netatmo("id");
var co2 = netatmoDevice.co2();
var humidity = netatmoDevice.humidity();
var noise = netatmoDevice.noise();
var pressure = netatmoDevice.pressure();
var temperature = netatmoDevice.temperature();

var netatmoDevices = netatmo_list.all();
var netatmoDevices = netatmo_list.byRoom("Living room");

// Philips Hue gateway
var hueDevice = philipshue("id");
hueDevice.on();
hueDevice.off();
hueDevice.on(transitionTimeInSeconds);
hueDevice.off(transitionTimeInSeconds);
hueDevice.color("#ff0000");
hueDevice.color("#ff0000", transitionTimeInSeconds);
hueDevice.brightness(0.5);
hueDevice.brightness(0.5, transitionTimeInSeconds);
hueDevice.temperature(2000); // 2000 <= x <= 6500

var hueDevices = philipshue_list.all();
var hueDevices = philipshue_list.byRoom("Living room");

// SONOS gateway
var sonosDevice = sonos("id");
sonosDevice.play();
sonosDevice.pause();
sonosDevice.stop();
sonosDevice.radio("url of stream", "name of radio station");
sonosDevice.file("file in network", "title", "album");
var volume = sonosDevice.volume();
sonosDevice.volume(0.3);
var isMaster = sonosDevice.master();

if (sonosDevice.state() === 'PLAYING') {
    sonosDevice.stop();
}

var sonosDevices = sonos_list.all();
var sonosDevices = sonos_list.byRoom("Living room");

// messaging
pushalot.send("Hi!") // via pushalot.com
sms.send("+41790000000", "Hi!"); // via aspsms.com
```
