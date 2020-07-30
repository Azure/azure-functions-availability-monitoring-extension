const appInsights = require('applicationinsights');
const https = require('https');

appInsights.setup().start();

module.exports = function (context, myTimer) {
    if (myTimer.isPastDue) {
        context.log('JavaScript is running late!');
    }

    const options = {
        hostname: 'availabilitymonitoring-extension-monitoredappsample.azurewebsites.net',
        port: 443,
        path: '/Home/MonitoredPage',
        method: 'GET'
    };

    var request = https.request(options, (response) => {
        const message = `Request completed with the status code: ${response.statusCode}`;
        console.log(message);
        // do custom logic to report success only for successfull status codes
        const success = response.statusCode === 200;
        context.bindings.availabilityTelemetry = {
            success: success,
            message: message
        };
        context.done();
    }).on('error', error => {
        const errorMessage = `Failed to execute the request: ${error.message}`;
        console.error(errorMessage);
        context.bindings.availabilityTelemetry = {
            success: false,
            message: errorMessage
        };
        context.done();
    });

    request.end();

    context.log('JavaScript timer trigger function ran!');
};