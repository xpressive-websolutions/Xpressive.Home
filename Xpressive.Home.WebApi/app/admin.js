(function () {

    var xha = angular.module("admin", ["ngRoute", "ui.bootstrap", "ui.codemirror"]);

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
            .when("/rooms/groups", {
                templateUrl: "/app/admin/scriptgroups.html",
                controller: "scriptGroupController"
            });
    });

    xha.filter("titlecase", function () {
        return function (input) {
            return input.toUpperCase()[0] + input.substr(1);
        }
    });

    xha.controller("navigationController", ["$rootScope", "$location", function ($rootScope, $location) {
        var c = this;

        $rootScope.$on("$locationChangeSuccess", function() {
            c.path = $location.path();
        });
    }]);

    xha.controller("deviceController", ["$scope", "$log", "$http", "$location", "$uibModal", function ($scope, $log, $http, $location, $uibModal) {
        var map = {};
        $scope.gateways = [];
        $scope.gatewaysWithFactory = [];

        var getDevices = function (gateway) {
            $http.get("/api/v1/gateway/" + gateway.name, { cache: false }).then(function (deviceResult) {
                var devices = _.sortBy(deviceResult.data, function (d) { return d.name; });
                gateway.devices = devices;
            });
        };

        $http.get("/api/v1/gateway", { cache: false }).then(function(result) {
            var gateways = _.sortBy(result.data, function (g) { return g.name; });

            _.each(gateways, function (g) {
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

        $scope.addDevice = function(gatewayName) {
            $http.get("/api/v1/gateway/" + gatewayName + "/empty").then(function(result) {
                var modal = $uibModal.open({
                    animation: false,
                    templateUrl: "/app/admin/createDeviceDialog.html",
                    controller: "createDeviceController",
                    resolve: {
                        device: function () {
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
        $log.debug(angular.toJson($scope.keys));

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

        $http.get("/api/v1/variable/" + gateway + "/" + deviceId, { cache: false }).then(function(result) {
            $scope.variables = result.data;
        });

        $http.get("/api/v1/gateway/" + gateway, { cache: false }).then(function (result) {
            var device = _.find(result.data, function (d) { return d.id === deviceId; });
            
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

        $scope.openScript = function (id) {
            $location.path("/scripts/" + id);
        }

        $scope.createScript = function() {
            var modal = $uibModal.open({
                animation: false,
                templateUrl: "/app/admin/singleInputDialog.html",
                controller: "singleInputController",
                resolve: {
                    caption: function () {
                        return "Create Script";
                    },
                    label: function () {
                        return "Name";
                    }
                }
            });

            modal.result.then(function (script) {
                $http.post("/api/v1/script", "'" + script + "'").then(function (result) {
                    $scope.openScript(result.data.id.replace("-", ""));
                });
            });
        };
    }]);

    xha.controller("singleInputController", ["$scope", "$log", "$uibModalInstance", "caption", "label", function($scope, $log, $uibModalInstance, caption, label) {
        $scope.caption = caption;
        $scope.label = label;

        $scope.value = "";

        $scope.ok = function () {
            $uibModalInstance.close($scope.value);
        };

        $scope.cancel = function () {
            $uibModalInstance.dismiss("cancel");
        };
    }]);

    xha.controller("scriptDetailController", ["$scope", "$log", "$http", "$routeParams", "$uibModal", function($scope, $log, $http, $routeParams, $uibModal) {
        var id = $routeParams.id;

        $http.get("/api/v1/script/" + id, { cache: false }).then(function(result) {
            $scope.script = result.data;
        });

        $scope.save = function () {
            $http.post("/api/v1/script/" + id, $scope.script);
        };
    }]);

})();
