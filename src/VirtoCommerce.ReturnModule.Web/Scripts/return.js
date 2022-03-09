var moduleName = 'virtoCommerce.returnModule';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(
        ['$stateProvider', function ($stateProvider) {
            $stateProvider
                .state('workspace.ReturnState', {
                    url: '/Return',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        '$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                            var newBlade = {
                                id: 'returnsList',
                                controller: 'virtoCommerce.returnModule.returnListController',
                                template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/return-list.tpl.html',
                                isClosingDisabled: true
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }]
    )
    .run(['platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state',
        function (mainMenuService, widgetService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/Return',
                icon: 'fa fa-exchange',
                title: 'Return',
                priority: 100,
                action: function () { $state.go('workspace.ReturnState'); },
                permission: 'return:access'
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
