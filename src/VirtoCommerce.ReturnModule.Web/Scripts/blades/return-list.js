angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.returnListController', ['$scope', 'virtoCommerce.returnModule.returns', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'platformWebApp.ui-grid.extension',
        function ($scope, returns, bladeUtils, uiGridHelper, gridOptionExtension) {
            $scope.uiGridConstants = uiGridHelper.uiGridConstants;

            var blade = $scope.blade;
            blade.title = 'return.blades.return-list.title';
            blade.headIcon = 'fa fa-exchange';

            blade.refresh = function () {
                blade.isLoading = true;
                var searchCriteria = getSearchCriteria();

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

                                orderReturn.itemCount = orderReturn.lineItems.length ?? 0;
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
