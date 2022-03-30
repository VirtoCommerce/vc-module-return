angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.returnDetailsController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.settings', 'virtoCommerce.customerModule.members', 'virtoCommerce.customerModule.memberTypesResolverService', 'virtoCommerce.orderModule.statusTranslationService', 'platformWebApp.accounts', 'platformWebApp.objCompareService', 'virtoCommerce.returnModule.returns',
        ($scope, $translate, bladeNavigationService, dialogService, settings, members, memberTypesResolverService, statusTranslationService, accounts, objCompareService, returns) => {
            $scope.saveChanges = () => {
                angular.copy(blade.currentEntity, blade.originalEntity);

                returns.update(blade.currentEntity,
                    (data) => {
                        var newBlade = {
                            id: 'returnsList',
                            controller: 'virtoCommerce.returnModule.returnListController',
                            template: 'Modules/$(VirtoCommerce.Return)/Scripts/blades/return-list.tpl.html',
                            isClosingDisabled: true,
                            orderId: blade.currentEntity.order.id
                        };
                        bladeNavigationService.showBlade(newBlade);
                    });
            };

            var blade = $scope.blade;

            settings.getValues({ id: 'Return.Status' }, translateBladeStatuses);

            blade.refresh = () => {
                returns.get({ id: blade.currentEntityId },
                    (data) => {
                        blade.currentEntity = data;
                        blade.originalEntity = angular.copy(blade.currentEntity);

                        $translate('return.blades.return-details.title', { number: data.number }).then((translationResult) => {
                            blade.title = translationResult;
                        });

                        blade.isLoading = false;
                    });
            };

            blade.metaFields = [
                {
                    name: 'number',
                    isRequired: true,
                    isReadOnly: true,
                    title: "return.blades.return-details.labels.number",
                    valueType: "ShortText"
                },
                {
                    name: 'order.number',
                    isReadOnly: true,
                    title: "return.blades.return-details.labels.orderNumber",
                    valueType: "ShortText"
                },
                {
                    name: 'createdDate',
                    isReadOnly: true,
                    title: "return.blades.return-details.labels.createdDate",
                    valueType: "DateTime"
                },
                {
                    name: 'modifiedDate',
                    isReadOnly: true,
                    title: "return.blades.return-details.labels.modifiedDate",
                    valueType: "DateTime"
                },
                {
                    name: 'createdBy',
                    title: "return.blades.return-details.labels.createdBy",
                    templateUrl: 'creatorSelector.html'
                },
                {
                    name: 'customer',
                    title: "return.blades.return-details.labels.customer",
                    templateUrl: 'customerSelector.html'
                },
                {
                    name: 'status',
                    templateUrl: 'statusSelector.html'
                },
                {
                    name: 'resolution',
                    isRequired: false,
                    title: "return.blades.return-details.labels.resolution",
                    valueType: "LongText"
                }
            ];

            blade.toolbarCommands = [
                {
                    name: "platform.commands.refresh", icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: () => true
                },
                {
                    name: "platform.commands.save",
                    icon: 'fas fa-save',
                    executeMethod: $scope.saveChanges,
                    canExecuteMethod: canSave,
                    permission: blade.updatePermission
                },
                {
                    name: "platform.commands.reset", icon: 'fa fa-undo',
                    executeMethod: () => {
                        angular.copy(blade.originalEntity, blade.currentEntity);
                    },
                    canExecuteMethod: isDirty,
                    permission: blade.updatePermission
                }
            ];

            blade.openStatusSettingManagement = () => {
                var newBlade = {
                    id: 'settingDetailChild',
                    isApiSave: true,
                    currentEntityId: 'Return.Status',
                    parentRefresh: translateBladeStatuses,
                    controller: 'platformWebApp.settingDictionaryController',
                    template: '$(Platform)/Scripts/app/settings/blades/setting-dictionary.tpl.html'
                };
                bladeNavigationService.showBlade(newBlade, blade);
            };

            blade.openCreatorDetails = () => {
                accounts.get({ id: blade.currentEntity.createdBy }, (account) => {
                    if (account && account.memberId) {
                        members.get({ id: account.memberId }, (member) => {
                            if (member && member.id) {
                                showCustomerDetailBlade(member);
                            }
                        });
                    }
                });
            };

            blade.openCustomerDetails = () => {
                members.get({ id: blade.currentEntity.order.customerId }, (member) => {
                    if (member && member.id) {
                        showCustomerDetailBlade(member);
                    }
                });

            };

            function translateBladeStatuses(data) {
                blade.statuses = statusTranslationService.translateStatuses(data, 'return');
            }

            function canSave() {
                return isDirty() && (!$scope.formScope || $scope.formScope.$valid);
            }

            function isDirty() {
                return blade.originalEntity && !objCompareService.equal(blade.originalEntity, blade.currentEntity) && !blade.isNew && blade.hasUpdatePermission();
            }

            function showCustomerDetailBlade(member) {
                var foundTemplate = memberTypesResolverService.resolve(member.memberType);
                if (foundTemplate) {
                    var newBlade = angular.copy(foundTemplate.detailBlade);
                    newBlade.currentEntity = member;
                    bladeNavigationService.showBlade(newBlade, blade);
                } else {
                    dialogService.showNotificationDialog({
                        id: "error",
                        title: "customer.dialogs.unknown-member-type.title",
                        message: "customer.dialogs.unknown-member-type.message",
                        messageValues: { memberType: member.memberType }
                    });
                }
            }

            blade.refresh();
        }]);
