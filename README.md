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

### Helpers

```javascript
// wait some milliseconds
var durationInMilliseconds = 200;
sleep(durationInMilliseconds);

// execute other scripts
execute("script ID");
execute("script ID", delayInMilliseconds);

// write to the logfile
log.debug("this is a debug message");
log.info("this is an info message");
log.warning("this is a warning message");
log.error("this is an error message");
```

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
pushover.send("Hi!", "USER_KEY"); // via pushover.net
pushalot.send("Hi!") // via pushalot.com
sms.send("+41790000000", "Hi!"); // via aspsms.com
```

## Gateways

Currently implemented gateways

### Daylight

Add locations manually by submitting Latitude and Longitude as well as an offset in minutes. Daylight gateway will compute the specific sunrise and sunset time as well as the current status (isDaylight).

No global configuration needed.

### Denon

The denon gateway search autonomously for denon devices. It's possible to switch the source or change the volume. You can switch it on or off or you can mute and unmute it.

No global configuration needed.

### Email

Not implemented yet.

### Forecast

Uses the `forecast.io` api to get the current weather forecast for a given location. Locations are created manually by providing Longitude and Latitude.

You have to add your forecast ApiKey to the config file:

```xml
<appSettings>
    <add key="forecast.apikey" value="YOUR API KEY HERE" />
</appSettings>
```

### LIFX

Scans your local network for LIFX bulbs. You can provide your cloud credentials too:

```xml
<appSettings>
    <add key="lifx.token" value="YOUR TOKEN HERE" />
</appSettings>
```

### myStrom

Scans your local network for myStrom devices. If you named your devices with the website, you can add your website credentials to show the correct name.

```xml
<appSettings>
    <add key="mystrom.username" value="YOUR USERNAME HERE" />
    <add key="mystrom.password" value="YOUR PASSWORD HERE" />
</appSettings>
```

### Netatmo

This gateway grabs the device information from the cloud. 

```xml
<appSettings>
    <add key="netatmo.clientid" value="YOUR NETATMO CLIENTID HERE" />
    <add key="netatmo.clientsecret" value="YOUR NETATMO CLIENTSECRET HERE" />
    <add key="netatmo.username" value="YOUR NETATMO USERNAME HERE" />
    <add key="netatmo.password" value="YOUR NETATMO PASSWORD HERE" />
</appSettings>
```

### Philips Hue

If the gateway founds a new bridge, you are asked (only shown in the admin panel) to push the button.

No global configuration needed.

### Pushalot

Create a `pushalot.com` account to send push notification to your smartphone. This gateway does not create any device. You can send push notifications with scripts.

```xml
<appSettings>
    <add key="pushalot.token" value="YOUR PUSHALOT TOKEN HERE" />
</appSettings>
```

### SMS

Create a `aspsms.com` account to send SMS notifications. Like the pushalot gateway, this gateway doesn't create any device. Use scripts to send notifications.

```xml
<appSettings>
    <add key="sms.username" value="YOUR ASPSMS.COM USERNAME HERE" />
    <add key="sms.password" value="YOUR ASPSMS.COM PASSWORD HERE" />
</appSettings>
```

### Sonos

Scans your local network for sonos devices.

No global configuration needed.

### Z-Wave

Uses a Z-Wave USB Dongle you can add your Z-Wave devices. Provide the COM-Port settings to the config file:

```xml
<appSettings>
    <add key="zwave.port" value="Z-WAVE PORT GOES HERE (i.e. COM3)" />
</appSettings>
```
