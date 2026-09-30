angular.module('virtoCommerce.returnModule')
    // A merchant's translation of the Return.Status dictionary first, then the label the module ships.
    .filter('returnStatusTranslate', ['$translate', 'platformWebApp.localizableSettingService', ($translate, localizableSettingService) => (status) => {
        if (!status) {
            return status;
        }

        var translated = localizableSettingService.translate(status, 'Return.Status');

        if (translated !== status) {
            return translated;
        }

        var key = 'settings.Return.Status.' + status;
        var label = $translate.instant(key);

        return label === key ? status : label;
    }]);
