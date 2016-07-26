﻿var storefrontApp = angular.module('storefrontApp');

storefrontApp.component('vcCheckoutPaymentMethods', {
	templateUrl: "themes/assets/js/checkout/checkout-paymentMethods.tpl.html",
	require: {
		checkoutStep: '^vcCheckoutWizardStep'
	},
	bindings: {
		getAvailPaymentMethods: '&',
		onSelectMethod: '&',
		paymentMethod: '='
	},
	controller: ['$scope', function ($scope) {
		var ctrl = this;

		this.$onInit = function () {
			ctrl.getAvailPaymentMethods().then(function (methods) {
				ctrl.availPaymentMethods = _.sortBy(methods, function (x) { return x.priority; });
				if (ctrl.paymentMethod) {
					ctrl.paymentMethod = _.find(ctrl.availPaymentMethods, function (x) { return x.gatewayCode == ctrl.paymentMethod.gatewayCode; })
				}
				if (!ctrl.paymentMethod && ctrl.availPaymentMethods.length > 0)
				{
					ctrl.paymentMethod = ctrl.availPaymentMethods[0];
					//ctrl.onSelectMethod({ paymentMethod : ctrl.paymentMethod }); // can't pass parameter???
				}
			})
			ctrl.checkoutStep.addComponent(this);
		};

		this.$onDestroy = function () {
			ctrl.checkoutStep.removeComponent(this);
		};
	
		ctrl.validate = function () {
			return ctrl.paymentMethod;
		}
	}]
});
