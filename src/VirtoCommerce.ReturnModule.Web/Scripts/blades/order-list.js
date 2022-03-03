angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.orderListController', ['$rootScope', '$scope', 'virtoCommerce.orderModule.order_res_customerOrders', 'platformWebApp.bladeUtils', 'platformWebApp.authService', 'uiGridConstants', 'platformWebApp.uiGridHelper', 'platformWebApp.ui-grid.extension', '$translate',
        function ($rootScope, $scope, customerOrders, bladeUtils, authService, uiGridConstants, uiGridHelper, gridOptionExtension, $translate) {
            var blade = $scope.blade;
            blade.title = 'return.blades.order-list.title';

            var bladeNavigationService = bladeUtils.bladeNavigationService;

            $scope.uiGridConstants = uiGridConstants;

            $scope.getPricesVisibility = () => authService.checkPermission('order:read_prices');

            $scope.getGridOptions = () => {
                return {
                    useExternalSorting: true,
                    data: 'objects',
                    rowTemplate: 'order-list.row.html',
                    columnDefs: [
                        { name: 'number', displayName: 'orders.blades.customerOrder-list.labels.number', width: '***', displayAlways: true },
                        { name: 'customerName', displayName: 'orders.blades.customerOrder-list.labels.customer', width: '***' },
                        { name: 'storeId', displayName: 'orders.blades.customerOrder-list.labels.store', width: '**' },
                        { name: 'total', displayName: 'orders.blades.customerOrder-list.labels.total', cellFilter: 'currency | showPrice:' + $scope.getPricesVisibility(), width: '**' },
                        { name: 'currency', displayName: 'orders.blades.customerOrder-list.labels.currency', width: '*' },
                        { name: 'isApproved', displayName: 'orders.blades.customerOrder-list.labels.confirmed', width: '*', cellClass: '__blue' },
                        { name: 'status', displayName: 'orders.blades.customerOrder-list.labels.status', cellFilter: 'statusTranslate:row.entity', width: '*' },
                        { name: 'createdDate', displayName: 'orders.blades.customerOrder-list.labels.created', width: '**', sort: { direction: uiGridConstants.DESC } }
                    ]
                }
            }

            $rootScope.$on('loginStatusChanged', (securityScopes) => {
                $translate.refresh().then(() => {
                    let gridOptions = $scope.getGridOptions();
                    $scope.setGridOptions("customerOrder-list-grid", gridOptions);
                });
            });

            blade.refresh = function () {
                if (angular.isFunction(blade.refreshCallback)) {
                    blade.isLoading = true;

                    var result = blade.refreshCallback(blade);

                    if (angular.isDefined(result.$promise)) {
                        result.$promise.then(function (data) {
                            blade.isLoading = false;

                            $scope.pageSettings.totalItems = data.totalCount;
                            $scope.objects = data.results;
                        });
                    }
                }
                else if (blade.preloadedOrders) {
                    $scope.pageSettings.totalItems = blade.preloadedOrders.length;
                    $scope.objects = blade.preloadedOrders;

                    blade.isLoading = false;
                } else {
                    blade.isLoading = true;
                    var criteria = {
                        responseGroup: "WithPrices",
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount
                    };

                    if (blade.searchCriteria) {
                        angular.extend(criteria, blade.searchCriteria);
                    }

                    customerOrders.search(criteria, function (data) {
                        blade.isLoading = false;

                        $scope.pageSettings.totalItems = data.totalCount;
                        $scope.objects = data.results;
                    });
                }
            };

            $scope.selectNode = function (node) {
                var itemsListBlade = {
                    id: 'itemListBlade',
                    controller: 'virtoCommerce.returnModule.orderItemsController',
                    template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/items-list.tpl.html',
                    isClosingDisabled: false,
                    hideDelete: true,
                    currentEntity: { id: node.id }
                };

                bladeNavigationService.showBlade(itemsListBlade, blade);
            };

            blade.headIcon = 'fa fa-file-text';

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
                Array.prototype.push.apply(gridOptions.columnDefs, _.map([
                    "discountAmount", "subTotal", "subTotalWithTax", "subTotalDiscount", "subTotalDiscountWithTax", "subTotalTaxTotal",
                    "shippingTotal", "shippingTotalWithTax", "shippingSubTotal", "shippingSubTotalWithTax", "shippingDiscountTotal", "shippingDiscountTotalWithTax", "shippingTaxTotal",
                    "paymentTotal", "paymentTotalWithTax", "paymentSubTotal", "paymentSubTotalWithTax", "paymentDiscountTotal", "paymentDiscountTotalWithTax", "paymentTaxTotal",
                    "discountTotal", "discountTotalWithTax", "fee", "feeWithTax", "feeTotal", "feeTotalWithTax", "taxTotal", "sum"
                ], function (name) {
                    return { name: name, cellFilter: "currency | showPrice:" + $scope.getPricesVisibility(), visible: false };
                }));

                $scope.gridOptions = gridOptions;
                gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);

                uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
                    if (blade.preloadedOrders) {
                        $scope.gridOptions.enableSorting = true;
                        $scope.gridOptions.useExternalSorting = false;
                    }
                    else {
                        uiGridHelper.bindRefreshOnSortChanged($scope);
                    }
                });

                bladeUtils.initializePagination($scope);

                return gridOptions;
            };
        }]);