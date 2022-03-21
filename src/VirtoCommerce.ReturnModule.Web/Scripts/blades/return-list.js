angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.returnListController', ['$scope', 'virtoCommerce.returnModule.returns', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'platformWebApp.ui-grid.extension',
        function ($scope, returns, bladeUtils, uiGridHelper, gridOptionExtension) {
            $scope.uiGridConstants = uiGridHelper.uiGridConstants;

            var blade = $scope.blade;
            var bladeNavigationService = bladeUtils.bladeNavigationService;

            blade.title = 'return.blades.return-list.title';
            blade.headIcon = 'fa fa-exchange';

            blade.refresh = function () {
                blade.isLoading = true;
                var searchCriteria = getSearchCriteria();

                if (blade.orderId) {
                    searchCriteria.orderId = blade.orderId;
                }

                if (blade.searchCriteria) {
                    angular.extend(searchCriteria, blade.searchCriteria);
                }

                returns.search(searchCriteria,
                    function (data) {
                        blade.isLoading = false;
                        $scope.pageSettings.totalItems = data.totalCount;

                        if (Array.isArray(data.results) && data.results.length) {
                            _.each(data.results, function (orderReturn) {
                                if (orderReturn.order) {
                                    orderReturn.orderNumber = orderReturn.order.number ?? '';
                                    orderReturn.customerName = orderReturn.order.customerName ?? '';
                                }
                                else {
                                    orderReturn.orderNumber = '';
                                    orderReturn.customerName = '';
                                }

                                orderReturn.itemsCount = orderReturn.lineItems.length ?? 0;
                            });
                        }

                        $scope.listEntries = data.results ? data.results : [];
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
                    name: "return.blades.return-list.labels.add-return", icon: 'fas fa-plus',
                    executeMethod: function (currentBlade) {
                        var orderListBlade = {
                            id: 'orderListBlade',
                            controller: 'virtoCommerce.returnModule.orderListController',
                            template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/order-list.tpl.html',
                            isClosingDisabled: false,
                            hideDelete: true,
                            isExpanded: true
                        };

                        bladeNavigationService.showBlade(orderListBlade, currentBlade);
                    },
                    canExecuteMethod: function () {
                        return true;
                    }
                }
            ];

            $scope.setGridOptions = function (gridId, gridOptions) {
                $scope.gridOptions = gridOptions;
                gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);

                gridOptions.onRegisterApi = function (gridApi) {
                    $scope.gridApi = gridApi;
                    gridApi.core.on.sortChanged($scope, function () {
                        if (!blade.isLoading) blade.refresh();
                    });
                };

                bladeUtils.initializePagination($scope);
            };

            $scope.clearKeyword = function () {
                blade.searchKeyword = null;
                blade.refresh();
            };

            $scope.selectNode = function (node) {
                $scope.selectedNodeId = node.id;

                var returnDetailsBlade = {
                    id: 'returnDetailsBlade',
                    controller: 'virtoCommerce.returnModule.returnDetailsController',
                    template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/return-details.tpl.html',
                    isClosingDisabled: false,
                    hideDelete: true,
                    isExpanded: true
                };

                returnDetailsBlade.currentEntityId = node.id;
                bladeNavigationService.showBlade(returnDetailsBlade, blade);
            };

            function getSearchCriteria() {
                return {
                    keyword: blade.searchKeyword,
                    responseGroup: "WithOrders",
                    sort: uiGridHelper.getSortExpression($scope),
                    skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                    take: $scope.pageSettings.itemsPerPageCount
                };
            }
        }]);
