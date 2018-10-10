(function(jq, _) {

    var xh = angular.module("xpressivehome", ["ngRoute", "ui.bootstrap", "rzModule", "rx"]);

    // TODO: sidebar mit "pinned" scripts z.B. für alarm scharf stellen, also raumübergreiffend

    xh.controller("dateTimeController", ["$log", "$interval", function($log, $interval) {
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

    xh.controller("roomController", ["$rootScope", "$log", "$http", function($rootScope, $log, $http) {
        var c = this;

        c.rooms = [];
        c.groups = [];
        c.room = null;

        c.selectRoom = function(room) {
            c.room = room;

            $rootScope.$broadcast("selectedRoomChanged", room);

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
            var offsets = jq("#" + caller)[0].getBoundingClientRect();
            var dd = jq("#" + caller).find(".dropdown-menu")[0];
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
            "clear-night",
            "partly-cloudy-night",
            "clear-day",
            "wind",
            "partly-cloudy-day",
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
                    c.forecast[0].prefixes.push("");

                    $http.get("/api/v1/variable/Weather?deviceId=" + device.id, { cache: false }).then(function(variables) {
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
                                c.forecast[i + 1].prefixes.push("H+" + h + "_");
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

    xh.controller("musicController", ["$rootScope", "$scope", "$log", "$http", "$uibModal", "$interval", "$q", "observeOnScope", function($rootScope, $scope, $log, $http, $uibModal, $interval, $q, observeOnScope) {
        var c = this;
        var devices = [];
        var roomDevices = [];
        var selectedRoom = null;
        var nextPlayingUpdate = new Date();

        c.isEnabled = false;
        c.device = null;
        c.isPlaying = false;
        c.deviceVolume = 0;
        $scope.volume = 0;

        observeOnScope($scope, "volume").sample(1000).subscribe(function (change) {
            if (Math.abs(c.deviceVolume - change.newValue) < 2) {
                return;
            }

            nextPlayingUpdate = new Date();
            nextPlayingUpdate.setSeconds(nextPlayingUpdate.getSeconds() + 10);
            c.deviceVolume = change.newValue;

            if (c.device) {
                $http.post("/api/v1/radio/volume?deviceId=" + encodeURIComponent(c.device.id) + "&volume=" + c.deviceVolume);
            }
        });

        var selectRoomDevice = function () {
            c.isEnabled = false;
            c.device = null;

            if (devices.length > 0 && roomDevices.length > 0 && selectedRoom) {
                var roomId = selectedRoom.id.replace(/-/g, "");
                _.each(roomDevices, function(rd) {
                    if (rd.roomId.replace(/-/g, "") === roomId) {
                        var device = _.find(devices, function(d) { return d.id === rd.deviceId; });
                        if (device) {
                            c.isEnabled = true;
                            c.device = device;
                        }
                    }
                });
            }
        };

        $http.get("/api/v1/gateway", { cache: false }).then(function(gateways) {
            if (!_.find(gateways.data, function(g) { return g.name === "Sonos"; })) {
                return;
            }

            var devicePromise = $http.get("/api/v1/gateway/Sonos", { cache: false });
            var roomDevicePromise = $http.get("/api/v1/roomdevice/Sonos", { cache: false });

            $q.all([devicePromise, roomDevicePromise]).then(function(result) {
                devices = result[0].data;
                roomDevices = result[1].data;
                selectRoomDevice();
            });
        });

        $rootScope.$on("selectedRoomChanged", function(event, data) {
            selectedRoom = data;
            selectRoomDevice();
        });

        c.sliderOptions = {
            floor: 0,
            ceil: 100,
            boundPointerLabels: false,
            hidePointerLabels: true,
            hideLimitLabels: true,
            interval: 1000
        };

        c.togglePlay = function () {
            if (!c.device) {
                return;
            }

            if (c.isPlaying) {
                $http.post("/api/v1/radio/stop?deviceId=" + encodeURIComponent(c.device.id)).then(function () {
                    c.isPlaying = false;
                });
            } else {
                $http.post("/api/v1/radio/play?deviceId=" + encodeURIComponent(c.device.id)).then(function() {
                    c.isPlaying = true;
                });
            }
        };

        c.selectStation = function() {
            var modalInstance = $uibModal.open({
                templateUrl: "/app/musicSelectionController.min.html",
                controller: "musicSelectionController"
            });

            modalInstance.result.then(
                function(selectedItem) {
                    if (selectedItem.id === c.stationId && c.isPlaying) {
                        return;
                    }

                    nextPlayingUpdate = new Date();
                    nextPlayingUpdate.setSeconds(nextPlayingUpdate.getSeconds() + 10);

                    c.stationId = selectedItem.id;
                    c.station = selectedItem.name;
                    c.imageUrl = selectedItem.imageUrl;
                    c.playing = "";

                    if (c.device) {
                        $http.post("/api/v1/radio/play/radio?deviceId=" + encodeURIComponent(c.device.id), selectedItem);
                    }
                },
                function() {
                    c.stationId = null;
                    c.station = null;
                    c.imageUrl = null;
                    c.playing = "";
                });
        };

        $interval(function() {
            if (nextPlayingUpdate > new Date()) {
                $log.debug("skip");
                return;
            }

            if (c.device) {
                $http.get("/api/v1/variable/Sonos?deviceId=" + encodeURIComponent(c.device.id)).then(function(result) {
                    _.each(result.data, function(d) {
                        if (d.name === "Volume") {
                            c.deviceVolume = d.value;
                            $scope.volume = d.value;
                        } else if (d.name === "TransportState") {
                            c.isPlaying = d.value === "Playing";
                        } else if (d.name === "CurrentUri") {
                            var r = /x-(?:rincon-mp3radio:\/\/|sonosapi-stream:)(s[0-9]+)\?.*/g;
                            var result = r.exec(d.value);
                            if (result && result.length === 2) {
                                c.stationId = result[1];
                            }
                        }
                    });
                });
            }
        }, 10000);

        $interval(function() {
            if (c.stationId && c.isPlaying) {
                $http.get("/api/v1/radio/playing?stationId=" + c.stationId, { cache: false }).then(function(result) {
                    if (result.data) {
                        c.imageUrl = result.data.playingImageUrl;
                        c.playing = result.data.playing;
                        c.station = result.data.name;
                    } else {
                        c.imageUrl = null;
                        c.playing = "";
                        c.station = "";
                    }
                },
                function() {
                    c.imageUrl = null;
                    c.playing = "";
                });
            }
        }, 10000);
    }]);

    xh.controller("musicSelectionController", ["$scope", "$http", "$uibModalInstance", function($scope, $http, $uibModalInstance) {
        $scope.stations = {};
        $scope.favorites = [];
        $scope.categories = [];
        $scope.query = "";
        $scope.selected = {};
        $scope.isOkEnabled = false;

        $http.get("/api/v1/radio/starred", { cache: false }).then(function(result) {
            $scope.favorites = result.data;
        });

        $http.get("/api/v1/radio/category").then(function(result) {
            $scope.categories = result.data;
        });

        $scope.search = function() {
            $scope.favorites = [];
            if ($scope.query) {
                $http.get("/api/v1/radio/search?query=" + encodeURIComponent($scope.query)).then(function(result) {
                    $scope.stations = result.data;
                });
            }
        };

        $scope.star = function(station) {
            $http.put("/api/v1/radio/star", station);
        };

        $scope.unstar = function(station) {
            $http.put("/api/v1/radio/unstar", station).then(function() {
                $scope.favorites.splice($scope.favorites.indexOf(station), 1);
            });
        };

        $scope.selectCategory = function(category) {
            $scope.favorites = [];
            $http.get("/api/v1/radio/category?parentId=" + category.id).then(function(result) {
                $scope.categories = result.data;
            });
            $http.get("/api/v1/radio/station?categoryId=" + category.id).then(function(result) {
                $scope.stations = result.data;
            });
        };

        $scope.selectStation = function(station) {
            $scope.selected = station;
            $scope.isOkEnabled = true;
        }

        $scope.selectMoreStations = function() {
            if (!$scope.stations.showMore) {
                return;
            }
            var showMore = $scope.stations.showMore;
            $scope.stations.showMore = null;
            $http.get("/api/v1/radio/station?categoryId=" + showMore).then(function(result) {
                _.each(result.data.stations, function(s) {
                    $scope.stations.stations.push(s);
                });
                $scope.stations.showMore = result.data.showMore;
            });
        };

        $scope.ok = function() {
            $uibModalInstance.close($scope.selected);
        };

        $scope.cancel = function() {
            $uibModalInstance.dismiss("cancel");
        };
    }]);

})($, _);
