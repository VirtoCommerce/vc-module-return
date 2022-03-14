angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.orderItemsController', ['$scope', '$translate', 'platformWebApp.authService', 'virtoCommerce.returnModule.returns', 'platformWebApp.bladeUtils', 'virtoCommerce.orderModule.order_res_customerOrders', 'platformWebApp.objCompareService',
        function ($scope, $translate, authService, returns, bladeUtils, customerOrders, objCompareService) {
            var blade = $scope.blade;
            blade.updatePermission = 'order:update';
            blade.isVisiblePrices = authService.checkPermission('order:read_prices');

            var bladeNavigationService = bladeUtils.bladeNavigationService;
            var selectedItems = [];
            blade.title = 'return.blades.items-list.title';

            blade.refresh = function () {
                blade.isLoading = true;

                if (blade.editMode) {
                    returns.get({ id: blade.currentEntity.id },
                        function (result) {
                            result.lineItems.forEach(item => {
                                var orderItem = result.order.items.find(x => x.id === item.orderLineItemId);

                                item.name = orderItem.name;
                                item.sku = orderItem.sku;
                            });

                            blade.currentEntity = result;
                            blade.originalEntity = angular.copy(blade.currentEntity);

                            $scope.lineItems = blade.currentEntity.lineItems;

                            $translate('return.blades.items-list.subtitle-edit', { number: result.number }).then(function (translationResult) {
                                blade.subtitle = translationResult;
                            });

                            blade.isLoading = false;
                            blade.selectedAll = false;
                        });
                } else {
                    customerOrders.get({ id: blade.currentEntity.order.id },
                        function (result) {
                            returns.availableQuantities({ id: blade.currentEntity.order.id },
                                function(data) {
                                    result.items.forEach(item => item.quantity = item.availableQuantity = data[item.id]);

                                    blade.currentEntity.order = result;

                                    $scope.lineItems = blade.currentEntity.order.items;

                                    $translate('return.blades.items-list.subtitle-create', { number: result.number }).then(function (translationResult) {
                                        blade.subtitle = translationResult;
                                    });

                                    blade.isLoading = false;
                                    blade.selectedAll = false;
                                });
                        });
                }
            };

            blade.toolbarCommands = [
                {
                    name: "platform.commands.refresh", icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: function () {
                        return true;
                    }
                }
            ];

            var button = blade.editMode ?
                {
                    name: "platform.commands.save",
                    icon: 'fas fa-save',
                    executeMethod: function() {
                        returns.update(blade.currentEntity,
                            function(data) {
                                bladeNavigationService.closeBlade(blade);
                            });
                    },
                    canExecuteMethod: function () {
                        return !objCompareService.equal(blade.originalEntity, blade.currentEntity);
                    },
                    permission: blade.updatePermission
                }
                :
                {
                    name: "Make return",
                    icon: 'fa fa-exchange',
                    executeMethod: function () {
                        var orderReturn = {
                            orderId: blade.currentEntity.order.id,
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
                                    id: 'returnDetailsBlade',
                                    controller: 'virtoCommerce.returnModule.returnDetailsController',
                                    template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/return-details.tpl.html',
                                    isClosingDisabled: false,
                                    hideDelete: true,
                                    isExpanded: true,
                                    currentEntityId: data.id
                                };

                                bladeNavigationService.closeBlade(blade);
                                bladeNavigationService.showBlade(newBlade);

                            });
                    },
                    canExecuteMethod: function () {
                        return selectedItems.length > 0;
                    },
                    permission: blade.updatePermission
                };

            blade.toolbarCommands.push(button);

            $scope.checkAll = function (selected) {
                angular.forEach($scope.lineItems, function (item) {
                    item.selected = selected;
                });
                $scope.updateSelectionList();
            };

            $scope.updateSelectionList = function () {
                selectedItems = $scope.lineItems.filter(function (item) {
                    return item.selected && item.quantity > 0;
                });
            };

            blade.refresh();
        }]);
