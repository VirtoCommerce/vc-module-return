angular.module('virtoCommerce.returnModule')
    .controller('virtoCommerce.returnModule.returnDetailsController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.settings', 'virtoCommerce.customerModule.members', 'virtoCommerce.customerModule.memberTypesResolverService', 'virtoCommerce.orderModule.statusTranslationService', 'platformWebApp.accounts', 'platformWebApp.objCompareService', 'virtoCommerce.returnModule.returns',
        ($scope, $translate, bladeNavigationService, dialogService, settings, members, memberTypesResolverService, statusTranslationService, accounts, objCompareService, returns) => {

            var blade = $scope.blade;

            $scope.saveChanges = () => {
                returns.update(blade.currentEntity,
                    (data) => {
                        angular.copy(blade.currentEntity, blade.originalEntity);
                        refreshAvailableStatuses();
                        if (blade.listRefresh) {
                            blade.listRefresh();
                        }
                    });
            };
            
            var statusSettingValues = [];
            var availableStatuses = [];

            settings.getValues({ id: 'Return.Status' }, (data) => {
                statusSettingValues = data;
                translateBladeStatuses();
            });

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

                refreshAvailableStatuses();
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
                    name: 'customerReference',
                    isReadOnly: true,
                    title: "return.blades.return-details.labels.customerReference",
                    valueType: "ShortText"
                },
                {
                    name: 'resolution',
                    isRequired: false,
                    title: "return.blades.return-details.labels.resolution",
                    valueType: "LongText"
                },
                {
                    // Recorded when the return is approved or declined, from the line items blade.
                    name: 'rejectReason',
                    isReadOnly: true,
                    isRequired: false,
                    title: "return.blades.return-details.labels.rejectReason",
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
                    parentRefresh: (data) => {
                        statusSettingValues = data;
                        refreshAvailableStatuses();
                    },
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

            // What an edit accepts depends on the return's decision as well as its status, so the server says.
            function refreshAvailableStatuses() {
                returns.availableStatuses({ id: blade.currentEntityId }, (data) => {
                    availableStatuses = data;
                    translateBladeStatuses();
                });
            }

            function translateBladeStatuses() {
                blade.statuses = statusTranslationService.translateStatuses(statusSettingValues, 'return')
                    .filter(x => availableStatuses.indexOf(x.key) >= 0);
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
