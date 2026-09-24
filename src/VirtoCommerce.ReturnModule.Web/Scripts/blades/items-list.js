angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.orderItemsController', ['$scope', '$translate', 'platformWebApp.authService', 'virtoCommerce.returnModule.returns', 'platformWebApp.bladeUtils', 'virtoCommerce.orderModule.order_res_customerOrders', 'platformWebApp.objCompareService',
        ($scope, $translate, authService, returns, bladeUtils, customerOrders, objCompareService) => {
            var refreshReturnList = function () {
                var orderBlade = $scope.$parent.$parent.blades.find(x => x.id === "returnsList");

                if (orderBlade) {
                    orderBlade.refresh();
                }
            }

            var blade = $scope.blade;
            blade.updatePermission = 'return:update';
            blade.authorizePermission = 'return:authorize';
            blade.isVisiblePrices = authService.checkPermission('order:read_prices');

            var bladeNavigationService = bladeUtils.bladeNavigationService;
            var selectedItems = [];
            blade.title = 'return.blades.items-list.title';

            blade.refresh = () => {
                blade.isLoading = true;

                if (blade.editMode) {
                    returns.get({ id: blade.currentEntity.id },
                        (result) => {
                            result.lineItems.forEach(item => {
                                var orderItem = result.order.items.find(x => x.id === item.orderLineItemId);

                                item.name = orderItem.name;
                                item.sku = orderItem.sku;
                            });

                            // A requested return, or one raised here as New, is waiting for a decision.
                            // Until one is made, every line is offered as approved in full, so
                            // declining is deliberate.
                            blade.canAuthorize = ['Requested', 'New'].indexOf(result.status) >= 0 && authService.checkPermission(blade.authorizePermission);

                            if (blade.canAuthorize) {
                                result.lineItems.forEach(item => {
                                    item.approvedQuantity = item.quantity;
                                });
                            }

                            blade.currentEntity = result;
                            blade.originalEntity = angular.copy(blade.currentEntity);

                            $scope.lineItems = blade.currentEntity.lineItems;

                            $translate('return.blades.items-list.subtitle-edit', { number: result.number }).then((translationResult) => {
                                blade.subtitle = translationResult;
                            });

                            blade.isLoading = false;
                            blade.selectedAll = false;
                        });
                } else {
                    customerOrders.get({ id: blade.currentEntity.order.id },
                        (result) => {
                            returns.availableQuantities({ id: blade.currentEntity.order.id },
                                (data) => {
                                    result.items.forEach(item => item.quantity = item.availableQuantity = data[item.id]);

                                    blade.currentEntity.order = result;

                                    $scope.lineItems = blade.currentEntity.order.items;

                                    $translate('return.blades.items-list.subtitle-create', { number: result.number }).then((translationResult) => {
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
                    canExecuteMethod: () => true
                }
            ];

            var button = blade.editMode ?
                {
                    name: "platform.commands.save",
                    icon: 'fas fa-save',
                    executeMethod: () => {
                        returns.update(blade.currentEntity,
                            (data) => {
                                bladeNavigationService.closeBlade(blade);
                            });
                    },
                    canExecuteMethod: () => !objCompareService.equal(withoutDecision(blade.originalEntity), withoutDecision(blade.currentEntity)),
                    permission: blade.updatePermission
                }
                :
                {
                    name: "Make return",
                    icon: 'fa fa-exchange',
                    executeMethod: () => {
                        var orderReturn = {
                            orderId: blade.currentEntity.order.id,
                            status: "New",
                            lineItems: selectedItems.map((item) => 
                                ({
                                    orderLineItemId: item.id,
                                    price: item.price,
                                    quantity: item.quantity,
                                    reason: item.reason ?? null
                                })
                            )
                        }

                        returns.update(orderReturn,
                            (data) => {
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

                                refreshReturnList();
                            });
                    },
                    canExecuteMethod: () => selectedItems.length > 0,
                    permission: blade.updatePermission
                };

            blade.toolbarCommands.push(button);

            if (blade.editMode) {
                blade.toolbarCommands.push({
                    name: 'return.blades.items-list.commands.authorize',
                    icon: 'fas fa-check',
                    executeMethod: () => {
                        blade.isLoading = true;

                        returns.authorize({ id: blade.currentEntity.id }, {
                            rejectReason: blade.currentEntity.rejectReason,
                            items: blade.currentEntity.lineItems.map(item => ({
                                lineItemId: item.id,
                                approvedQuantity: item.approvedQuantity,
                                rejectReason: item.rejectReason
                            }))
                        }, () => {
                            if (blade.parentBlade && blade.parentBlade.refresh) {
                                blade.parentBlade.refresh();
                            }

                            refreshReturnList();
                            blade.refresh();
                        }, () => {
                            blade.isLoading = false;
                        });
                    },
                    // An emptied field is null, not a decision to decline; a fraction is not a quantity.
                    canExecuteMethod: () => blade.canAuthorize && blade.currentEntity.lineItems.every(item =>
                        Number.isInteger(item.approvedQuantity) && item.approvedQuantity >= 0 && item.approvedQuantity <= item.quantity),
                    permission: blade.authorizePermission
                });
            }

            // The decision is recorded by authorizing; saving never writes it.
            function withoutDecision(orderReturn) {
                var result = angular.copy(orderReturn);

                if (result && result.lineItems) {
                    delete result.rejectReason;
                    result.lineItems.forEach(item => {
                        delete item.approvedQuantity;
                        delete item.rejectReason;
                    });
                }

                return result;
            }

            $scope.checkAll = (selected) => {
                angular.forEach($scope.lineItems, (item) => {
                    item.selected = selected;
                });
                $scope.updateSelectionList();
            };

            $scope.updateSelectionList = () => {
                selectedItems = $scope.lineItems.filter((item) => item.selected && item.quantity > 0);
            };

            blade.refresh();
        }]);
