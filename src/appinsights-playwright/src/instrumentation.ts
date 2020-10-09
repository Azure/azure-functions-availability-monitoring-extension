const BrowserContext = require('playwright-chromium/lib/server/browserContext');

export type ScreenshotMode = 'AutoCollect' | 'OnFailure' | 'No';

interface WebTestStep {
    action: string;    
    url: string;
    resultCode: string;
    success: boolean;    
    duration: number;
    timestamp: number;
    screenshot?: string;
    target?: string;
    elementValue?: string;
}

export class AppInsightsContextListener {
    _actionListener: AppInsightsActionListener;    
    constructor(state?: ScreenshotMode) {
        this._actionListener = new AppInsightsActionListener(state || 'No');
        BrowserContext.contextListeners.add(this);        
    }

    dispose() {
        BrowserContext.contextListeners.delete(this);
    }

    async onContextCreated(context: any): Promise<void> {        
        // subscribe new context to actions listening
        context._actionListeners.add(this._actionListener);
    }

    async onContextDestroyed(context: any): Promise<void> {        
    }

    serializeData() {
        const data = {
            type: 'playwright',
            steps: this._actionListener._data
        };
        return {        
            body: JSON.stringify(data),
            status: this._actionListener._failed ? '500' : '200'
        };
    }
}

export class AppInsightsActionListener {
    _state: ScreenshotMode;
    _data: WebTestStep[] = [];
    _failed: boolean;
    constructor(state: ScreenshotMode) {
        this._state = state;
        this._failed = false;
    }
    async onAfterAction(result: any, metadata: any): Promise<void> {
        try {
            const pageUrl = metadata.page.mainFrame().url();           

            this._failed = this._failed || !!result.error;

            // Track new step on completion
            let step: WebTestStep = {
                action: metadata.type,
                target: metadata.target,
                elementValue: metadata.value,
                resultCode: !!result.error ? '500' : '200',
                success: !result.error,
                url: pageUrl,
                duration: result.endTime - result.startTime,
                timestamp: result.startTime
            }

            switch (this._state) {
                case 'AutoCollect': {
                    const buffer = await metadata.page.screenshot({ type: 'jpeg' });
                    step.screenshot = buffer.toString('base64');
                    break;
                }
                case 'OnFailure': {
                    if (!!result.error) {
                        const buffer = await metadata.page.screenshot({ type: 'jpeg' });
                        step.screenshot = buffer.toString('base64');
                    }
                    break;
                }
                default:
                    break;
            }

            this._data.push(step);
        } catch (e) {
            // Do not throw from instrumentation.
            console.log('Error during appinsights instrumentation: ' + e.message);            
        }
    }
}