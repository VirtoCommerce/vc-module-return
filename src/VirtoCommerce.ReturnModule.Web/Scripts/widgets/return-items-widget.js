angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.returnItemsWidgetController', ['$scope', 'platformWebApp.bladeNavigationService',
        ($scope, bladeNavigationService) => {
            var blade = $scope.widget.blade;

            $scope.$watch('widget.blade.currentEntity', (orderReturn) => {
                $scope.orderReturn = orderReturn;

                if (orderReturn) {
                    var total = orderReturn.lineItems.reduce((partialSum, x) => partialSum + (x.quantity * x.price), 0);

                    total = Math.round((total + Number.EPSILON) * 100) / 100;

                    $scope.total = total + " " + orderReturn.order.currency;
                }
            });

            $scope.openItemsBlade = () => {
                var itemsListBlade = {
                    id: 'itemListBlade',
                    controller: 'virtoCommerce.returnModule.orderItemsController',
                    template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/items-list.tpl.html',
                    isClosingDisabled: false,
                    hideDelete: true,
                    currentEntity: { id: $scope.orderReturn.id },
                    editMode: true
                };

                bladeNavigationService.showBlade(itemsListBlade, blade);
            };
        }]);
