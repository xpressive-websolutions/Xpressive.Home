(function(_) {

    var xha = angular.module("admin", ["ngRoute", "ui.bootstrap", "ui.codemirror", "ui.select", "ngSanitize", "toaster"]);

    xha.config(function($routeProvider) {
        $routeProvider
            .when("/", {
                templateUrl: "/app/admin/devices.html",
                controller: "deviceController"
            })
            .when("/variables/:gateway/:device", {
                templateUrl: "/app/admin/variables.html",
                controller: "variableController"
            })
            .when("/scripts", {
                templateUrl: "/app/admin/scripts.html",
                controller: "scriptController"
            })
            .when("/scripts/:id", {
                templateUrl: "/app/admin/script.html",
                controller: "scriptDetailController"
            })
            .when("/rooms", {
                templateUrl: "/app/admin/rooms.html",
                controller: "roomController"
            })
            .when("/rooms/:id", {
                templateUrl: "/app/admin/room.html",
                controller: "roomDetailController"
            })
            .when("/group/:id", {
                templateUrl: "/app/admin/scriptgroup.html",
                controller: "scriptGroupController"
            });
    });

    xha.filter("titlecase", function() {
        return function(input) {
            return input.toUpperCase()[0] + input.substr(1);
        };
    });

    xha.controller("installController", ["$http", function($http) {
        var c = this;

        c.yes = function() {
            $http.post("/api/v1/softwareupdate/start");
        };
    }]);

    xha.controller("updateController", ["toaster", "$http", "$interval", "$timeout", function(toaster, $http, $interval, $timeout) {
        var showToast = function() {
            toaster.pop("info", "Update available", "softwareUpdateTemplate.html", null, "template");
        };

        var checkForUpdate = function() {
            $http.get("/api/v1/softwareupdate/hasNewVersion", { cache: false }).then(function(result) {
                if (result.data === true) {
                    showToast();
                }
            });
        };

        $timeout(checkForUpdate, 1 * 60 * 1000);
        $interval(checkForUpdate, 60 * 60 * 1000);
    }]);

    xha.controller("navigationController", ["$rootScope", "$location", function($rootScope, $location) {
        var c = this;

        $rootScope.$on("$locationChangeSuccess", function() {
            c.path = $location.path();
        });
    }]);

    xha.controller("deviceController", ["$scope", "$log", "$http", "$location", "$uibModal", function($scope, $log, $http, $location, $uibModal) {
        var map = {};
        $scope.gateways = [];
        $scope.gatewaysWithFactory = [];

        var getDevices = function(gateway) {
            $http.get("/api/v1/gateway/" + gateway.name, { cache: false }).then(function(deviceResult) {
                var devices = _.sortBy(deviceResult.data, function(d) { return d.name; });
                gateway.devices = devices;

                $http.get("/api/v1/roomdevice/" + gateway.name, { cache: false }).then(function(result) {
                    _.each(result.data, function(p) {
                        var room = _.find($scope.rooms, function(r) { return r.id === p.roomId; });
                        var device = _.find(gateway.devices, function(d) { return d.id === p.deviceId; });
                        if (room && device) {
                            device.room = room;
                        }
                    });
                });
            });
        };

        $http.get("/api/v1/room", { cache: false }).then(function(result) {
            $scope.rooms = result.data;
        });

        $http.get("/api/v1/gateway", { cache: false }).then(function(result) {
            var gateways = _.sortBy(result.data, function(g) { return g.name; });

            _.each(gateways, function(g) {
                var gateway = {
                    name: g.name,
                    canCreateDevices: g.canCreateDevices,
                    devices: []
                };

                map[g.name] = gateway;

                $scope.gateways.push(gateway);

                if (g.canCreateDevices) {
                    $scope.gatewaysWithFactory.push(gateway.name);
                }
            });

            _.each($scope.gateways, getDevices);
        });

        $scope.showVariables = function(gatewayName, deviceId) {
            $location.path("/variables/" + gatewayName + "/" + deviceId);
        };

        $scope.roomSelected = function(gateway, device, room) {
            var dto = {
                gatewayName: gateway.name,
                deviceId: device.id,
                roomId: room ? room.id : ""
            };

            if (!room) {
                device.room = null;
            }

            $http.post("/api/v1/roomdevice", dto);
        };

        $scope.addDevice = function(gatewayName) {
            $http.get("/api/v1/gateway/" + gatewayName + "/empty").then(function(result) {
                var modal = $uibModal.open({
                    animation: false,
                    templateUrl: "/app/admin/createDeviceDialog.html",
                    controller: "createDeviceController",
                    resolve: {
                        device: function() {
                            return result.data;
                        },
                        gateway: function() {
                            return gatewayName;
                        }
                    }
                });

                modal.result.then(function(device) {
                    $http.post("/api/v1/gateway/" + gatewayName, device).then(function() {
                        var gateway = map[gatewayName];
                        getDevices(gateway);
                    });
                });
            });
        };
    }]);

    xha.controller("createDeviceController", ["$scope", "$log", "$uibModalInstance", "gateway", "device", function($scope, $log, $uibModalInstance, gateway, device) {
        $scope.device = device;
        $scope.gateway = gateway;

        $scope.keys = _.keys(device);

        $scope.ok = function() {
            $uibModalInstance.close(device);
        };

        $scope.cancel = function() {
            $uibModalInstance.dismiss("cancel");
        };
    }]);

    xha.controller("variableController", ["$scope", "$routeParams", "$log", "$http", function($scope, $routeParams, $log, $http) {
        var gateway = $routeParams.gateway;
        var deviceId = $routeParams.device;
        var deviceIdEncoded = encodeURIComponent(deviceId);

        $http.get("/api/v1/variable/" + gateway + "?deviceId=" + deviceIdEncoded, { cache: false }).then(function(result) {
            $scope.variables = result.data;
        });

        $http.get("/api/v1/gateway/" + gateway, { cache: false }).then(function(result) {
            var device = _.find(result.data, function(d) { return d.id === deviceId; });
            
            if (device) {
                $scope.deviceName = device.name;
                $scope.gateway = gateway;
                $scope.deviceId = deviceId;
            }
        });
    }]);

    xha.controller("scriptController", ["$scope", "$log", "$http", "$location", "$uibModal", function($scope, $log, $http, $location, $uibModal) {
        $scope.scripts = [];

        $http.get("/api/v1/script", { cache: false }).then(function(result) {
            $scope.scripts = result.data;
        });

        $scope.enable = function(script) {
            $http.post("/api/v1/script/" + script.id + "/enable").then(function() {
                script.isEnabled = true;
            });
        };

        $scope.disable = function(script) {
            $http.post("/api/v1/script/" + script.id + "/disable").then(function() {
                script.isEnabled = false;
            });
        };

        $scope.execute = function(script) {
            $http.post("/api/v1/script/execute/" + script.id);
        };

        $scope.openScript = function(id) {
            $location.path("/scripts/" + id);
        };

        $scope.createScript = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/singleInputDialog.html",
                controller: "singleInputController",
                resolve: {
                    caption: function() {
                        return "Create Script";
                    },
                    label: function() {
                        return "Name";
                    },
                    value: function() {
                        return "";
                    }
                }
            });

            modal.result.then(function(script) {
                $http.post("/api/v1/script", "'" + script + "'").then(function(result) {
                    $scope.openScript(result.data.id.replace(/-/g, ""));
                });
            });
        };
    }]);

    xha.controller("singleInputController", ["$scope", "$log", "$uibModalInstance", "caption", "label", "value", function($scope, $log, $uibModalInstance, caption, label, value) {
        $scope.caption = caption;
        $scope.label = label;
        $scope.value = value;

        $scope.ok = function() {
            $uibModalInstance.close($scope.value);
        };

        $scope.cancel = function() {
            $uibModalInstance.dismiss("cancel");
        };
    }]);

    xha.controller("scriptDetailController", ["$scope", "$log", "$http", "$routeParams", "$uibModal", "$interval", function($scope, $log, $http, $routeParams, $uibModal, $interval) {
        var id = $routeParams.id;

        $scope.triggers = [];
        $scope.schedules = [];

        var getTriggers = function() {
            $http.get("/api/v1/trigger/" + id, { cache: false }).then(function(result) {
                _.each(result.data, function(nt) {
                    if (!_.find($scope.triggers, function(t) { return t.variable === nt.variable; })) {
                        nt.value = "";
                        $scope.triggers.push(nt);
                    }
                });
                _.each($scope.triggers, function(t) {
                    if (!_.find(result.data, function(nt) { return t.variable === nt.variable; })) {
                        var i = $scope.triggers.indexOf(t);
                        $scope.triggers.splice(i, 1);
                    }
                });
            });
        };

        var getSchedules = function() {
            $http.get("/api/v1/schedule/" + id, { cache: false }).then(function(result) {
                $scope.schedules = result.data;
            });
        };

        $http.get("/api/v1/script/" + id, { cache: false }).then(function(result) {
            $scope.script = result.data;
        });

        getTriggers();
        getSchedules();

        $scope.save = function() {
            $http.post("/api/v1/script/" + id, $scope.script);
        };

        $scope.execute = function() {
            $http.post("/api/v1/script/execute/" + id);
        };

        $scope.enable = function() {
            $http.post("/api/v1/script/" + id + "/enable").then(function() {
                $scope.script.isEnabled = true;
            });
        };

        $scope.disable = function() {
            $http.post("/api/v1/script/" + id + "/disable").then(function() {
                $scope.script.isEnabled = false;
            });
        };

        $scope.addTrigger = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/singleInputDialog.html",
                controller: "singleInputController",
                resolve: {
                    caption: function() {
                        return "Add Trigger";
                    },
                    label: function() {
                        return "Variable name";
                    },
                    value: function() {
                        return "";
                    }
                }
            });

            modal.result.then(function(result) {
                $http.post("/api/v1/trigger/" + id, "'" + result + "'").then(getTriggers);
            });
        };

        $scope.removeTrigger = function(trigger) {
            $http.delete("/api/v1/trigger/" + trigger.id).then(getTriggers);
        };

        $scope.addSchedule = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/singleInputDialog.html",
                controller: "singleInputController",
                resolve: {
                    caption: function() {
                        return "Add Schedule";
                    },
                    label: function() {
                        return "Cron tab";
                    },
                    value: function() {
                        return "";
                    }
                }
            });

            modal.result.then(function(result) {
                $http.post("/api/v1/schedule/" + id, "'" + result + "'").then(getSchedules);
            });
        };

        $scope.removeSchedule = function(schedule) {
            $http.delete("/api/v1/schedule/" + schedule.id).then(getSchedules);
        };

        var interval = $interval(function() {
            _.each($scope.triggers, function(t) {
                $http.get("/api/v1/variable/" + t.variable, { cache: false }).then(function(result) {
                    t.value = result.data.value;
                });
            });
        }, 5000);

        $scope.$on("$destroy", function() {
            $interval.cancel(interval);
        });
    }]);

    xha.controller("roomController", ["$scope", "$http", "$uibModal", "$location", function($scope, $http, $uibModal, $location) {
        $scope.rooms = [];

        var getRooms = function() {
            $http.get("/api/v1/room", { cache: false }).then(function(result) {
                $scope.rooms = result.data;

                _.each($scope.rooms, function(r) {
                    r.id = r.id.replace(/-/g, "");
                    if (r.icon === "") {
                        r.icon = "glyphicon glyphicon-align-justify";
                    }
                });
            });
        };

        getRooms();

        $scope.showRoom = function(id) {
            $location.path("/rooms/" + id);
        };

        $scope.addRoom = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/singleInputDialog.html",
                controller: "singleInputController",
                resolve: {
                    caption: function() {
                        return "Add room";
                    },
                    label: function() {
                        return "Name";
                    },
                    value: function() {
                        return "";
                    }
                }
            });

            modal.result.then(function(result) {
                $http.post("/api/v1/room", "'" + result + "'").then(getRooms);
            });
        };
    }]);

    xha.controller("roomDetailController", ["$scope", "$log", "$http", "$routeParams", "$uibModal", "$location", function($scope, $log, $http, $routeParams, $uibModal, $location) {
        var id = $routeParams.id;
        $scope.room = {};
        $scope.groups = [];

        $http.get("/api/v1/room/" + id, { cache: false }).then(function(result) {
            $scope.room = result.data;
        });

        $scope.save = function() {
            $http.put("/api/v1/room", $scope.room);
        };

        var getGroups = function() {
            $http.get("/api/v1/roomscriptgroup?roomId=" + id, { cache: false }).then(function(result) {
                $scope.groups = result.data;
            });
        };

        getGroups();

        $scope.openGroup = function(groupId) {
            groupId = groupId.replace(/-/g, "");
            $location.path("/group/" + groupId);
        };

        $scope.addScriptGroup = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/singleInputDialog.html",
                controller: "singleInputController",
                resolve: {
                    caption: function() {
                        return "Add room script group";
                    },
                    label: function() {
                        return "Name";
                    },
                    value: function() {
                        return "";
                    }
                }
            });

            modal.result.then(function(result) {
                var group = {
                    name: result
                };
                $http.post("/api/v1/roomscriptgroup/" + id, group).then(getGroups);
            });
        };
    }]);

    xha.controller("scriptGroupController", ["$scope", "$http", "$routeParams", "$uibModal", function($scope, $http, $routeParams, $uibModal) {
        var id = $routeParams.id;
        var allScripts = [];
        $scope.group = {};
        $scope.scripts = [];

        var getScripts = function() {
            $http.get("/api/v1/roomscript?groupId=" + id, { cache: false }).then(function(result) {
                $scope.scripts = result.data;

                _.each($scope.scripts, function(s) {
                    s.id = s.id.replace(/-/g, "");
                    s.scriptId = s.scriptId.replace(/-/g, "");
                    s.script = _.find(allScripts, function(a) { return a.id === s.scriptId; });
                });
            });
        };

        $http.get("/api/v1/roomscriptgroup/" + id).then(function(result) {
            $scope.group = result.data;
        });

        $http.get("/api/v1/script", { cache: false }).then(function(result) {
            allScripts = result.data;
            getScripts();
        });

        getScripts();

        $scope.addScript = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/roomScriptDialog.html",
                controller: "roomScriptController",
                resolve: {
                    caption: function() {
                        return "Add room script";
                    },
                    script: function() {
                        return { };
                    },
                    scripts: function() {
                        return allScripts;
                    }
                }
            });

            modal.result.then(function(result) {
                result.scriptId = result.script.id;
                result.groupId = id;
                $http.post("/api/v1/roomscript", result).then(getScripts);
            });
        };

        $scope.updateScript = function(script) {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/roomScriptDialog.html",
                controller: "roomScriptController",
                resolve: {
                    caption: function() {
                        return "Update room script";
                    },
                    script: function() {
                        return script;
                    },
                    scripts: function() {
                        return allScripts;
                    }
                }
            });

            modal.result.then(function(result) {
                result.scriptId = result.script.id;
                $http.post("/api/v1/roomscript/" + result.id, result).then(getScripts);
            });
        };
    }]);

    xha.controller("roomScriptController", ["$scope", "$uibModalInstance", "caption", "script", "scripts", function($scope, $uibModalInstance, caption, script, scripts) {
        $scope.script = script;
        $scope.scripts = scripts;
        $scope.caption = caption;

        $scope.ok = function() {
            $uibModalInstance.close($scope.script);
        };

        $scope.cancel = function() {
            $uibModalInstance.dismiss("cancel");
        };
    }]);

})(_);
