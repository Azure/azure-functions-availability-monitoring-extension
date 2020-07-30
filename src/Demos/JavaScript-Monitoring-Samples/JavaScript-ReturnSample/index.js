const appInsights = require('applicationinsights');
const axios = require("axios");

appInsights.setup().start();


module.exports = async function (context, myTimer) {
    context.log('JavaScript timer trigger function ran!');
    const response = await axios({
        url: "https://availabilitymonitoring-extension-monitoredappsample.azurewebsites.net/Home/MonitoredPage",
        method: "get"
    });

    const success = response.status === 200;
    return {
        success: success,
        message: response.statusText
    };
}