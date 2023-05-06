import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
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
  @Output() reloadPageEmitter: EventEmitter<boolean> = new EventEmitter();
  @Input() member: Member | MemberLike | undefined;
  @Input() likeButton: boolean = true;



  constructor(private memberService: MembersService, private toastr: ToastrService) { }

  ngOnInit(): void {
  }

  addLike(member: Member | MemberLike) {
    this.memberService.addLike(member.userName).subscribe({
      next: _ => this.toastr.success(`You have liked ${member.knownAs}`)
    })
  }

  removeLike(member: Member | MemberLike) {
    this.memberService.removeLike(member.userName).subscribe({
      next: _ => {
        this.toastr.success(`You have unliked ${member.knownAs}`);
        this.reloadPageEmitter.emit(true);
      }
    });
  }
}
