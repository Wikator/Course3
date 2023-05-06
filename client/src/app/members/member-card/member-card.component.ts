import { Component, Input, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Member } from '../../_models/member';
import { MemberLike } from '../../_models/memberLike';
import { MembersService } from '../../_services/members.service';

@Component({
  selector: 'app-member-card',
  templateUrl: './member-card.component.html',
  styleUrls: ['./member-card.component.css']
})
export class MemberCardComponent implements OnInit {
  @Input() member: Member | MemberLike | undefined;

  constructor(private memberService: MembersService, private toastr: ToastrService) { }

  ngOnInit(): void {
  }

  addLike(member: Member | MemberLike) {
    this.memberService.addLike(member.userName).subscribe({
      next: _ => this.toastr.success(`You have liked ${member.knownAs}`)
    })
  }
}
