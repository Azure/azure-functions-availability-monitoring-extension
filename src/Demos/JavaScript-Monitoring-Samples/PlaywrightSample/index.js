const { chromium } = require('playwright-chromium');
const { AppInsightsContextListener } = require('appinsights-playwright')

module.exports = async function (context, req) {
    context.log("Function entered.");

    // initialize AppInsightsListener to collect information about Playwright execution
    // set input parameter to:
    //   - 'AutoCollect' to collect screenshots after every action taken
    //   - 'OnFailure' to collect screenshots only for the failed actions
    //   - 'No' to skip the screenshots collection. Default value.
    const listener = new AppInsightsContextListener('AutoCollect');

    try {
        const browser = await chromium.launch();
        const browserContext = await browser.newContext();

        // Open new page
        const page = await browserContext.newPage();
        page.setDefaultTimeout(5000);

        // Go to https://www.bing.com/?toHttps=1&redig=6AF43A87E4114AFA9B74F780918EB668
        await page.goto('https://www.bing.com/?toHttps=1&redig=6AF43A87E4114AFA9B74F780918EB668');

        // Click //label[normalize-space(@aria-label)='Search the web']/*[local-name()="svg"]
        await page.click('//label[normalize-space(@aria-label)=\'Search the web\']/*[local-name()="svg"]');
        // assert.equal(page.url(), 'https://www.bing.com/search?q=Playwright+docs&form=QBLH&sp=-1&pq=playwright+docs&sc=6-15&qs=n&sk=&cvid=8ED88C73553942CAB8C76EE34BEC2C3B');

        // Click text="Playwright"
        await page.click('text="Playwright"');
        // assert.equal(page.url(), 'https://playwright.dev/');

        // Close page
        await page.close();

        // Close browser
        await browser.close();

    } finally {
        // Serialize collected data into the response
        context.res = listener.serializeData();
        context.done();
    }    
};
