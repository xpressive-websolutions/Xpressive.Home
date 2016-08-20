(function(jq) {

    var xh = angular.module("xpressivehome", ['ngRoute', 'ui.bootstrap']);

    // TODO: sidebar mit "pinned" scripts z.B. für alarm scharf stellen, also raumübergreiffend

    xh.controller("dateTimeController", ['$log', '$interval', function($log, $interval) {
        var c = this;

        $interval(function() {
            c.date = new Date();
        }, 1000);
    }]);

    xh.controller("backgroundController", ["$interval", "$scope", function($interval, $scope) {
        var c = this;
        var seasons = ["spring", "summer", "autumn", "winter"];
        var times = ["night", "morning", "afternoon", "evening"];

        var calculateClass = function() {
            var season = "";
            var time = "";
            var now = new Date();
            var month = now.getMonth();
            var hour = now.getHours();

            if (month < 2) { season = seasons[3]; }
            else if (month < 5) { season = seasons[0]; }
            else if (month < 8) { season = seasons[1]; }
            else if (month < 11) { season = seasons[2]; }
            else { season = seasons[3]; }

            if (hour < 6) { time = times[0]; }
            else if (hour < 12) { time = times[1]; }
            else if (hour < 18) { time = times[2]; }
            else { time = times[3]; }

            c.selection = season + "-" + time;
        };

        calculateClass();
        var interval = $interval(calculateClass, 10000);

        $scope.$on("$destroy", function() {
            $interval.cancel(interval);
        });
    }]);

    xh.controller("roomController", ["$log", "$http", function($log, $http) {
        var c = this;

        c.rooms = [];
        c.groups = [];
        c.room = null;

        c.selectRoom = function(room) {
            c.room = room;

            $http.get("/api/v1/roomscriptgroup?roomId=" + room.id, { cache: false }).then(function(result) {
                var groups = _.sortBy(result.data, function(g) { return g.sortOrder; });

                _.each(groups, function(g) {
                    g.scripts = [];
                });

                c.groups = groups;

                _.each(c.groups, function(g) {
                    $http.get("/api/v1/script/group/" + g.id, { cache: false }).then(function(scripts) {
                        g.scripts = scripts.data;
                    });
                });
            });
        };

        c.selectScript = function(script) {
            $http.post("/api/v1/script/execute/" + script.id);
        };

        c.toggle = function(caller) {
            var offsets = jq('#' + caller)[0].getBoundingClientRect();
            var dd = jq('#' + caller).find(".dropdown-menu")[0];
            var maxHeight = jq(window).height();
            var height = jq(dd).actualHeight() * 1.3 + 12;
            var hp = "90%";
            var tp = "5%";

            if (height < 120) {
                height = 120;
            }
            if (height < (maxHeight * 0.9)) {
                var h = height / maxHeight;
                var t = (1 - h) / 2;
                hp = Math.round(height) + "px";
                tp = Math.round(t * 100) + "%";
            }

            dd.style.left = offsets.left + "px";
            dd.style.height = hp;
            dd.style.top = tp;
        };

        $http.get("/api/v1/room", { cache: false }).then(function(rooms) {
            c.rooms = _.sortBy(rooms.data, function(r) { return r.sortOrder; });

            if (!c.room && c.rooms) {
                c.selectRoom(c.rooms[0]);
            }
        });
    }]);

    xh.controller("weatherController", ["$log", "$http", "$interval", "$scope", function($log, $http, $interval, $scope) {
        var c = this;
        var icons = [
            "clear-day",
            "clear-night",
            "wind",
            "partly-cloudy-day",
            "partly-cloudy-night",
            "fog",
            "cloudy",
            "rain",
            "snow",
            "sleet",
            "hail",
            "thunderstorm",
            "tornado"
        ];

        var createEmptyStructure = function() {
            return {
                name: "",
                tempMin: 1000,
                tempMax: -1000,
                icon: "",
                prefixes: []
            };
        };

        c.isEnabled = false;
        c.forecast = [];
        c.forecast.push(createEmptyStructure());
        c.forecast.push(createEmptyStructure());
        c.forecast.push(createEmptyStructure());
        c.forecast.push(createEmptyStructure());
        c.variables = [];

        var updateWeather = function() {
            $http.get("/api/v1/gateway/Weather", { cache: false }).then(function(devices) {
                if (devices.data) {
                    var device = devices.data[0];
                    c.isEnabled = true;
                    c.forecast[0].name = "Current";
                    c.forecast[0].prefixes.push("H+0.");

                    $http.get("/api/v1/variable/Weather/" + device.id, { cache: false }).then(function(variables) {
                        var now = new Date();
                        var hour = now.getHours();
                        var hoursUntilNextStep = 6 - (hour % 6);
                        var labels = ["Night", "Morning", "Afternoon", "Evening"];
                        var index = 0;
                        var dict = [];

                        if (hour < 6) { index = 0; }
                        else if (hour < 12) { index = 1; }
                        else if (hour < 18) { index = 2; }
                        else { index = 3; }

                        for (var i = 0; i < 3; i++) {
                            var start = (i * 6) + hoursUntilNextStep;
                            var stop = start + 6;
                            for (var h = start; h < stop; h++) {
                                c.forecast[i + 1].prefixes.push("H+" + h + ".");
                            }
                            c.forecast[i + 1].name = labels[(i + index + 1) % 4];
                        }

                        _.each(variables.data, function(v) {
                            dict[v.name] = v.value;
                        });

                        _.each(c.forecast, function(f) {
                            _.each(f.prefixes, function(p) {
                                var t = dict[p + "Temperature"];
                                var c = dict[p + "Icon"];

                                if (f.tempMin > t) {
                                    f.tempMin = t;
                                }
                                if (f.tempMax < t) {
                                    f.tempMax = t;
                                }
                                if (f.icon === "") {
                                    f.icon = c;
                                } else {
                                    var oi = icons.indexOf(f.icon);
                                    var ni = icons.indexOf(c);
                                    if (ni > oi) {
                                        f.icon = c;
                                    }
                                }
                            });
                            f.tempMin = Math.round(f.tempMin, 0);
                            f.tempMax = Math.round(f.tempMax, 0);
                            f.icon = "wi wi-forecast-io-" + f.icon;
                            f.prefixes = [];
                        });
                    });
                }
            }, function() {
                c.isEnabled = false;
            });
        };

        var interval = null;

        $http.get("/api/v1/gateway", { cache: false }).then(function(gateways) {
            if (!_.find(gateways.data, function(g) { return g.name === "Weather"; })) {
                return;
            }

            updateWeather();

            interval = $interval(updateWeather, 10 * 60 * 1000); // every 10minutes
        });

        $scope.$on("$destroy", function() {
            if (interval) {
                $interval.cancel(interval);
            }
        });
    }]);

    xh.controller("musicController", ["$log", "$http", function($log, $http) {
        var c = this;

        c.isEnabled = false;

        $http.get("/api/v1/gateway", { cache: false }).then(function(gateways) {
            if (!_.find(gateways.data, function(g) { return g === "Sonos"; })) {
                return;
            }

            c.isEnabled = true;
            $log.debug("Music is enabled");

            $http.get("/api/v1/gateway/Sonos", { cache: false }).then(function(devices) {
                $log.debug(angular.toJson(devices.data));
            });
        });
    }]);

})($);
