angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.orderItemsController', ['$scope', '$translate', 'platformWebApp.authService', 'virtoCommerce.returnModule.returns', 'platformWebApp.bladeUtils', 'virtoCommerce.orderModule.order_res_customerOrders',
        function ($scope, $translate, authService, returns, bladeUtils, customerOrders) {
            var blade = $scope.blade;
            blade.updatePermission = 'order:update';
            blade.isVisiblePrices = authService.checkPermission('order:read_prices');

            var bladeNavigationService = bladeUtils.bladeNavigationService;
            var selectedItems = [];
            blade.title = 'return.blades.items-list.title';
            
            blade.refresh = function () {
                blade.isLoading = true;

                customerOrders.get({ id: blade.currentEntity.id },
                    function (result) {
                        result.items.forEach(item => item.avaliableQuantity = item.quantity);

                        blade.currentEntity = result;

                        $translate('return.blades.items-list.subtitle', { number: result.number }).then(function (translationResult) {
                            blade.subtitle = translationResult;
                        });

                        blade.isLoading = false;
                        blade.selectedAll = false;
                    });
            };

            blade.toolbarCommands = [
                {
                    name: "platform.commands.refresh", icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: function () {
                        return true;
                    }
                },
                {
                    name: "Make return", icon: 'fa fa-exchange',
                    executeMethod: function () {
                        var orderReturn = {
                            orderId: blade.currentEntity.id,
                            status: "New",
                            lineItems: selectedItems.map(function (item) {
                                return {
                                    orderLineItemId: item.id,
                                    price: item.price,
                                    quantity: item.quantity,
                                    reason: item.reason ?? null
                                }
                            })
                        }

                        returns.update(orderReturn,
                            function (data) {
                                var newBlade = {
                                    id: 'returnsList',
                                    controller: 'virtoCommerce.returnModule.returnListController',
                                    template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/return-list.tpl.html',
                                    isClosingDisabled: true
                                };
                                bladeNavigationService.showBlade(newBlade);
                            });
                    },
                    canExecuteMethod: function () {
                        return selectedItems.length > 0;
                    },
                    permission: blade.updatePermission
                }
            ];

            $scope.checkAll = function (selected) {
                angular.forEach(blade.currentEntity.items, function (item) {
                    item.selected = selected;
                });
                $scope.updateSelectionList();
            };

            $scope.updateSelectionList = function () {
                selectedItems = blade.currentEntity.items.filter(function (item) {
                    return item.selected && item.quantity > 0;
                });
            };

            blade.refresh();
        }]);
