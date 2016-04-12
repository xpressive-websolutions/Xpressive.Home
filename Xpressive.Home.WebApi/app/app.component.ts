import {Component} from "angular2/core";
import {DashboardComponent} from "./dashboard/dashboard"
import {AdminComponent} from "./admin/admin"

// todo
// - app component
//   - dashboard [/]
//     - date
//     - time
//     - button groups
//     - buttons
//     - playbar
//     - ...
//   - admin [/admin]
//     - header
//     - navigation
//     - rooms, scripts, scriptgroups, gateways, devices

@Component({
    selector: "xh-app",
    templateUrl: "/app/app.component.html",
    directives: [DashboardComponent, AdminComponent]
})
export class AppComponent {

}
