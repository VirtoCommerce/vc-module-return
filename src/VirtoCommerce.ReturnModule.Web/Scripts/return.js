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
    .run(['platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state', 'platformWebApp.toolbarService', 'platformWebApp.bladeNavigationService',
        function (mainMenuService, widgetService, $state, toolBarService, bladeNavigationService) {
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

            var operationItemsWidget = {
                controller: 'virtoCommerce.returnModule.returnItemsWidgetController',
                template: 'Modules/$(VirtoCommerce.Return)/Scripts/widgets/return-items-widget.tpl.html'
            };
            widgetService.registerWidget(operationItemsWidget, 'returnDetailsWidgets');

            var relatedReturnsWidget = {
                controller: 'virtoCommerce.returnModule.relatedReturnsWidgetController',
                template: 'Modules/$(VirtoCommerce.Return)/Scripts/widgets/order-related-returns-widget.tpl.html'
            };
            widgetService.registerWidget(relatedReturnsWidget, 'customerOrderDetailWidgets');

            var makeReturnCommand = {
                name: 'return.blades.return-list.labels.create-return',
                icon: 'fa fa-exchange',
                index: 6,
                executeMethod: function(blade) {
                    var itemsListBlade = {
                        id: 'itemListBlade',
                        controller: 'virtoCommerce.returnModule.orderItemsController',
                        template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/items-list.tpl.html',
                        isClosingDisabled: false,
                        hideDelete: true,
                        currentEntity: {
                            order: {
                                id: blade.currentEntity.id
                            }
                        },
                        editMode: false
                    };

                    bladeNavigationService.showBlade(itemsListBlade, blade);
                },
                canExecuteMethod: function(blade) {
                    return true;
                }
            }

            toolBarService.register(makeReturnCommand, 'virtoCommerce.orderModule.operationDetailController');
        }
    ]);
