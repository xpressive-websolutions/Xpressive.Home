(function () {

    var xh = angular.module("xpressivehome");

    xh.controller("dateTimeController", ['$log', '$interval', function($log, $interval) {
        var c = this;

        $interval(function() {
            c.date = new Date();
        }, 1000);
    }]);

    xh.controller("roomController", ["$log", "$http", function($log, $http) {
        var c = this;

        c.rooms = [];
        c.groups = [];
        c.room = null;

        c.selectRoom = function (room) {
            c.room = room;
            c.groups = [];

            $http.get("/api/v1/roomscriptgroup/" + room.id, { cache: false }).then(function (result) {
                var groups = _.sortBy(result.data, function (g) { return g.sortOrder; });

                _.each(groups, function(g) {
                    g.scripts = [];
                });

                c.groups = groups;

                _.each(c.groups, function(g) {
                    $http.get("/api/v1/script/group/" + g.id, { cache: false }).then(function(scripts) {
                        g.scripts = scripts.data;

                        g.scripts = [{ id: 'asdf', name: 'demo' }];

                        $log.debug(angular.toJson(g.scripts));
                    });
                });
            });
        };

        c.selectScript = function(script) {
            $http.post("/api/v1/script/execute/" + script.id);
        };

        c.toggle = function (caller) {
            var offsets = jQuery('#' + caller)[0].getBoundingClientRect();
            var dd = jQuery('#' + caller).find(".dropdown-menu")[0];
            var maxHeight = jQuery(window).height();
            var height = jQuery(dd).actualHeight() * 1.3 + 12;
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

        $http.get("/api/v1/room", { cache: false }).then(function (rooms) {
            c.rooms = _.sortBy(rooms.data, function (r) { return r.sortOrder; });

            if (!c.room && c.rooms) {
                c.selectRoom(c.rooms[0]);
            }
        });
    }]);

    xh.controller("weatherController", ["$log", "$http", function($log, $http) {
        var c = this;

        c.isEnabled = false;

        $http.get("/api/v1/gateway", { cache: false }).then(function(gateways) {
            if (!_.find(gateways.data, function(g) { return g === "Weather"; })) {
                return;
            }

            c.isEnabled = true;
            $log.debug("Weather is enabled");

            $http.get("/api/v1/gateway/Weather", { cache: false }).then(function (devices) {
                if (devices.data) {
                    var device = devices.data[0];

                    $http.get("/api/v1/variable/Weather/" + device.id, { cache: false }).then(function(variables) {
                        //_.each(variables.data, function(v) {
                        //    $log.debug(v.name + "=" + v.value);
                        //});
                    });
                }
            });
        });
    }]);

    xh.controller("musicController", ["$log", "$http", function($log, $http) {
        var c = this;

        c.isEnabled = false;

        $http.get("/api/v1/gateway", { cache: false }).then(function (gateways) {
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

})();

