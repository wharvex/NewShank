	.text
	.attribute	4, 16
	.attribute	5, "rv64i2p0"
	.file	"main"
	.globl	start                           # -- Begin function start
	.p2align	2
	.type	start,@function
start:                                  # @start
	.cfi_startproc
# %bb.0:                                # %entry
	addi	sp, sp, -80
	.cfi_def_cfa_offset 80
	sd	ra, 72(sp)                      # 8-byte Folded Spill
	sd	s0, 64(sp)                      # 8-byte Folded Spill
	sd	s1, 56(sp)                      # 8-byte Folded Spill
	.cfi_offset ra, -8
	.cfi_offset s0, -16
	.cfi_offset s1, -24
	sd	zero, 48(sp)
	li	a0, 10
	sd	a0, 40(sp)
	sd	zero, 8(sp)
.LBB0_1:                                # %while.cond
                                        # =>This Inner Loop Header: Depth=1
	ld	s0, 8(sp)
	ld	a0, 40(sp)
	bge	s0, a0, .LBB0_3
# %bb.2:                                # %while.body
                                        #   in Loop: Header=BB0_1 Depth=1
	ld	a1, 8(sp)
	slli	a0, a1, 1
	sd	a0, 16(sp)
	addi	a1, a1, 1
	sd	a1, 8(sp)
	call	write@plt
	addi	a0, s0, 1
	sd	a0, 8(sp)
	j	.LBB0_1
.LBB0_3:                                # %while.end
	ld	a0, 48(sp)
.LBB0_4:                                # %for.condition
                                        # =>This Inner Loop Header: Depth=1
	sd	a0, 32(sp)
	ld	a0, 32(sp)
	ld	a1, 40(sp)
	bge	a0, a1, .LBB0_6
# %bb.5:                                # %for.body
                                        #   in Loop: Header=BB0_4 Depth=1
	ld	a0, 32(sp)
	mv	a1, a0
	call	__muldi3@plt
	sd	a0, 16(sp)
	call	write@plt
	ld	a0, 32(sp)
	addi	a0, a0, 1
	j	.LBB0_4
.LBB0_6:                                # %exit
	ld	a0, 40(sp)
	sd	a0, 8(sp)
.LBB0_7:                                # %while.cond3
                                        # =>This Inner Loop Header: Depth=1
	ld	s0, 48(sp)
	ld	a0, 8(sp)
	bge	s0, a0, .LBB0_9
# %bb.8:                                # %while.body4
                                        #   in Loop: Header=BB0_7 Depth=1
	ld	a0, 16(sp)
	ld	a1, 8(sp)
	sub	a0, a0, a1
	sd	a0, 16(sp)
	addi	a1, a1, -1
	sd	a1, 8(sp)
	call	write@plt
	addi	a0, s0, 1
	sd	a0, 48(sp)
	j	.LBB0_7
.LBB0_9:                                # %while.end5
	ld	a0, 40(sp)
.LBB0_10:                               # %for.condition10
                                        # =>This Inner Loop Header: Depth=1
	sd	a0, 24(sp)
	ld	a0, 24(sp)
	ld	a1, 48(sp)
	bge	a0, a1, .LBB0_12
# %bb.11:                               # %for.body11
                                        #   in Loop: Header=BB0_10 Depth=1
	ld	s0, 40(sp)
	ld	s1, 24(sp)
	mv	a0, s0
	mv	a1, s1
	call	__divdi3@plt
	add	a1, s1, a0
	mv	a0, s0
	call	__muldi3@plt
	sd	a0, 16(sp)
	call	write@plt
	ld	a0, 24(sp)
	addi	a0, a0, 1
	j	.LBB0_10
.LBB0_12:                               # %exit17
	li	a0, 0
	ld	ra, 72(sp)                      # 8-byte Folded Reload
	ld	s0, 64(sp)                      # 8-byte Folded Reload
	ld	s1, 56(sp)                      # 8-byte Folded Reload
	addi	sp, sp, 80
	ret
.Lfunc_end0:
	.size	start, .Lfunc_end0-start
	.cfi_endproc
                                        # -- End function
	.section	".note.GNU-stack","",@progbits
