// Call this to register your module to main application
var moduleName = 'Return';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider', '$urlRouterProvider',
        function ($stateProvider, $urlRouterProvider) {
            $stateProvider
                .state('workspace.ReturnState', {
                    url: '/Return',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        '$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                            var newBlade = {
                                id: 'blade1',
                                controller: 'Return.helloWorldController',
                                template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/hello-world.html',
                                isClosingDisabled: true
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state',
        function (mainMenuService, widgetService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/Return',
                icon: 'fa fa-cube',
                title: 'Return',
                priority: 100,
                action: function () { $state.go('workspace.ReturnState'); },
                permission: 'Return:access'
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
