angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.relatedReturnsWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.returnModule.returns',
        ($scope, bladeNavigationService, returns) => {
            var blade = $scope.widget.blade;

            $scope.$watch('widget.blade.currentEntity', (entity) => {
                var searchCriteria = {
                    skip: 0,
                    take: 0,
                    orderId: blade.customerOrder.id
                }

                returns.search(searchCriteria,
                    (searchResult) => {
                        $scope.returnsCount = searchResult.totalCount;
                    });
            });

            $scope.openBlade = () => {
                var newBlade = {
                    id: 'returnsList',
                    controller: 'virtoCommerce.returnModule.returnListController',
                    template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/return-list.tpl.html',
                    isClosingDisabled: false,
                    orderId: blade.customerOrder.id
                };
                bladeNavigationService.showBlade(newBlade);
            }
        }]);
