# Xpressive.Home

Home automation solution in .NET

## Main concepts

Xpressive.Home consists of these main concepts

- **Variable store**  
  The variable store collects all variables. Variables mostly are specific device property values. There are four types of variables: `string`, `double` and `boolean`.
- **Message bus**  
  The message bus is the communication layer. Components can publish messages and can get notified for specific message types. There are four message types: `UpdateVariable`, `Command`, `NotifyUser` and `LowBattery`.
- **Gateway**  
  A Gateway is a communication interface between devices and the home automation software. Some gateways search autonomous for devices, some don't. Gateways scans their devices periodically to update their status. Status changes are published with UpdateVariable messages through the message bus. The variable store is listening to these messages and persist them.
- **Script**  
  Scripts are written in JavaScript and triggered by time (with Cron Tabs) or by variable changes. If you want to switch on your light bulbs on sunset, scripts are the way to go.

## Scripts

### Variables
```javascript
variable.set("xpressive.home is cool", true);
var isCool = variable.get("xpressive.home is cool");
```

### Devices

Most gateways gives you the possibility to access the devices in JavaScript by its id.

```javascript
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

// myStrom gateway
var mystromDevice = mystrom("id");
mystromDevice.on();
mystromDevice.relay(true); // same as on();
mystromDevice.off();
mystromDevice.relay(false); // same as off();
var isOn = mystromDevice.relay();
var consumption = mystromDevice.power();

// Netatmo gateway
var netatmoDevice = netatmo("id");
var co2 = netatmoDevice.co2();
var humidity = netatmoDevice.humidity();
var noise = netatmoDevice.noise();
var pressure = netatmoDevice.pressure();
var temperature = netatmoDevice.temperature();

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
```

### Lists

```javascript
var denonDevices = denon_list.all();
var denonDevices = denon_list.byRoom("Living room");

var lifxDevices = lifx_list.all();
var lifxDevices = lifx_list.byRoom("Living room");

var mystromDevices = mystrom_list.all();
var mystromDevices = mystrom_list.byRoom("Living room");

var netatmoDevices = netatmo_list.all();
var netatmoDevices = netatmo_list.byRoom("Living room");

var hueDevices = philipshue_list.all();
var hueDevices = philipshue_list.byRoom("Living room");

var sonosDevices = sonos_list.all();
var sonosDevices = sonos_list.byRoom("Living room");
```

### Notification

```javascript
// messaging
pushalot.send("Hi!") // via pushalot.com
sms.send("+41790000000", "Hi!"); // via aspsms.com
```
