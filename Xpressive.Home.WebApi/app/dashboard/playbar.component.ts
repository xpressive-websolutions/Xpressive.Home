import { Component, ElementRef, AfterViewInit } from "angular2/core";

declare var jQuery: any;

@Component({
    selector: "xh-playbar",
    templateUrl: "/app/dashboard/playbar.component.html"
})
export class PlaybarComponent implements AfterViewInit {
    constructor(private el: ElementRef) {
    }

    ngAfterViewInit() {
        jQuery("#ex5").slider();
    }
}
