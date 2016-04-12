import {Component} from "angular2/core";
import {Observable} from "rxjs/Rx";
import {DateFormatPipe} from "angular2-moment/DateFormatPipe";

@Component({
    selector: "xh-datetime",
    pipes: [DateFormatPipe],
    templateUrl: "/app/dashboard/datetime.component.html"
})
export class DateTimeComponent {
    constructor() {
        this.date = new Date();
        Observable
            .interval(1000)
            .subscribe(() => {
                this.date = new Date();
            });
    }

    date: Date;
}
