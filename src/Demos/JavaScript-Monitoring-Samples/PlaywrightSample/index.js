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

        // Go to https://www.bing.com/?toHttps=1&redig=69CC3FCA85A84B3AAFA1D638964EA2B1
        await page.goto('https://www.bing.com/?toHttps=1&redig=69CC3FCA85A84B3AAFA1D638964EA2B1');

        // Click input[aria-label="Enter your search term"]
        await page.click('input[aria-label="Enter your search term"]');

        // Fill input[aria-label="Enter your search term"]
        await page.fill('input[aria-label="Enter your search term"]', 'Playwright');

        // Click //label[normalize-space(@aria-label)='Search the web']/*[local-name()="svg"]
        await page.click('//label[normalize-space(@aria-label)=\'Search the web\']/*[local-name()="svg"]');
        // assert.equal(page.url(), 'https://www.bing.com/search?q=Playwright&form=QBLH&sp=-1&pq=playwright&sc=8-10&qs=n&sk=&cvid=A5708CE6F75940C79891958DC561761B');

        // Click text="Playwright"
        await page.click('text="Playwright"');
        // assert.equal(page.url(), 'https://playwright.dev/');

        // Close page
        await page.close();

        // ---------------------
        await browserContext.close();
        await browser.close();

    } catch (err) {
        context.log.error(err);
    } finally {
        // Serialize collected data into the response
        context.res = listener.serializeData();
        context.done();
    }
};
