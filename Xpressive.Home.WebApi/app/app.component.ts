import {Component} from 'angular2/core';
import {Observable} from 'rxjs/Rx';
import {DateFormatPipe} from 'angular2-moment/DateFormatPipe';


@Component({
    selector: 'my-app',
    pipes: [DateFormatPipe],
    template: `
<div class="col-md-6 datetime-date">
    <div class="col-md-12">
        {{ time | amDateFormat:'dddd' }}
    </div>
    <div class="col-md-12">
        {{ time | amDateFormat:'DD. MMMM YYYY' }}
    </div>
</div>
<div class="col-md-4 datetime-time">
    {{ time | amDateFormat:'HH:mm' }}&nbsp;Uhr
</div>
`
})
export class AppComponent {
    constructor() {
        this.time = new Date();
        Observable
            .interval(1000)
            .subscribe(() => {
                this.time = new Date();
            });
    }

    time: Date;
}
