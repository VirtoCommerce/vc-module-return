angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.relatedReturnsWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.returnModule.returns',
        function ($scope, bladeNavigationService, returns) {
            var blade = $scope.widget.blade;

            $scope.$watch('widget.blade.currentEntity', function (entity) {
                var searchCriteria = {
                    skip: 0,
                    take: 0,
                    orderId: blade.customerOrder.id
                }

                returns.search(searchCriteria,
                    function(searchResult) {
                        $scope.returnsCount = searchResult.totalCount;
                    });
            });

            $scope.openBlade = function() {
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
